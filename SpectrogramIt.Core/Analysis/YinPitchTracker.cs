using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core.Analysis;

/// <summary>
/// YIN pitch tracker (de Cheveigné &amp; Kawahara, "YIN, a fundamental frequency estimator
/// for speech and music", 2002). Returns 0 Hz for unvoiced frames.
/// </summary>
public sealed class YinPitchTracker
{
    public double FMin { get; }
    public double FMax { get; }
    public int FrameSize { get; }
    public int HopSize { get; }
    public double Threshold { get; }

    public YinPitchTracker(
        double fMin = 50.0,
        double fMax = 1000.0,
        int frameSize = 4096,
        int hopSize = 512,
        double threshold = 0.1)
    {
        FMin = fMin;
        FMax = fMax;
        FrameSize = frameSize;
        HopSize = hopSize;
        Threshold = threshold;
    }

    public PitchPoint[] Track(float[] samples, int sampleRate)
    {
        int frameCount = Math.Max(0, (samples.Length - FrameSize) / HopSize + 1);
        var points = new PitchPoint[frameCount];
        var buffer = new float[FrameSize];

        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * HopSize;
            Array.Copy(samples, offset, buffer, 0, FrameSize);

            double time = (offset + FrameSize / 2.0) / sampleRate;
            double f0 = EstimateFrame(buffer, sampleRate);
            points[i] = new PitchPoint(time, f0);
        }
        return points;
    }

    public double EstimateFrame(float[] frame, int sampleRate)
    {
        int n = frame.Length;
        int tauMax = (int)(sampleRate / FMin);      // longest period
        int tauMin = (int)(sampleRate / FMax);      // shortest period
        tauMin = Math.Max(tauMin, 2);
        tauMax = Math.Min(tauMax, n - 1);

        // Difference function d(tau).
        var d = new double[tauMax + 1];
        for (int tau = 0; tau <= tauMax; tau++)
        {
            double sum = 0;
            for (int i = 0; i < n - tau; i++)
            {
                double diff = frame[i] - frame[i + tau];
                sum += diff * diff;
            }
            d[tau] = sum;
        }

        // Cumulative mean normalized difference function d'(tau).
        var cmnd = new double[tauMax + 1];
        cmnd[0] = 1.0;
        double running = 0;
        for (int tau = 1; tau <= tauMax; tau++)
        {
            running += d[tau];
            cmnd[tau] = running > 0 ? d[tau] * tau / running : 1.0;
        }

        // Find the first tau whose cmnd drops below threshold; else absolute min.
        int bestTau = FindTau(cmnd, tauMin, tauMax);

        if (bestTau <= 0 || bestTau > tauMax)
            return 0.0;

        // Parabolic interpolation for sub-sample precision.
        double refined = Interpolate(cmnd, bestTau);
        if (refined <= 0)
            return 0.0;

        return sampleRate / refined;
    }

    private int FindTau(double[] cmnd, int tauMin, int tauMax)
    {
        // The first dip below threshold within [tauMin, tauMax].
        for (int tau = tauMin; tau <= tauMax; tau++)
        {
            if (cmnd[tau] < Threshold)
            {
                // Step forward while the function continues to fall (find the valley).
                int t = tau;
                while (t + 1 <= tauMax && cmnd[t + 1] < cmnd[t])
                    t++;
                return t;
            }
        }
        // No dip below threshold: use global minimum.
        int minTau = tauMin;
        for (int tau = tauMin; tau <= tauMax; tau++)
        {
            if (cmnd[tau] < cmnd[minTau])
                minTau = tau;
        }
        return minTau;
    }

    private static double Interpolate(double[] y, int tau)
    {
        if (tau <= 0 || tau >= y.Length - 1)
            return tau;
        double a = y[tau - 1];
        double b = y[tau];
        double c = y[tau + 1];
        double denom = a - 2 * b + c;
        if (Math.Abs(denom) < 1e-12)
            return tau;
        double delta = 0.5 * (a - c) / denom;
        return tau + delta;
    }
}