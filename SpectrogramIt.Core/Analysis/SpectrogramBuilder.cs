using NWaves.Transforms;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core.Analysis;

/// <summary>Builds a frame-wise magnitude spectrogram over an audio buffer.</summary>
public sealed class SpectrogramBuilder
{
    public int FftSize { get; }
    public int HopSize { get; }

    public SpectrogramBuilder(int fftSize = 2048, int hopSize = 512)
    {
        FftSize = fftSize;
        HopSize = hopSize;
    }

    public SpectrogramFrame[] Build(float[] samples, int sampleRate)
    {
        var fft = new Fft(FftSize);
        var window = CreateHann(FftSize);
        var spectrum = new float[FftSize / 2 + 1];
        var frameSamples = new float[FftSize];
        double binWidth = (double)sampleRate / FftSize;

        int frameCount = Math.Max(0, (samples.Length - FftSize) / HopSize + 1);
        var frames = new SpectrogramFrame[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * HopSize;
            Array.Copy(samples, offset, frameSamples, 0, FftSize);

            // Apply Hann window.
            for (int k = 0; k < FftSize; k++)
                frameSamples[k] *= window[k];

            fft.MagnitudeSpectrum(frameSamples, spectrum);

            double time = (offset + FftSize / 2.0) / sampleRate;
            var mag = new double[FftSize / 2 + 1];
            for (int k = 0; k < mag.Length; k++)
                mag[k] = spectrum[k];
            frames[i] = new SpectrogramFrame(time, mag, binWidth);
        }
        return frames;
    }

    private static float[] CreateHann(int n)
    {
        var w = new float[n];
        for (int i = 0; i < n; i++)
            w[i] = 0.5f * (1 - (float)Math.Cos(2 * Math.PI * i / (n - 1)));
        return w;
    }
}