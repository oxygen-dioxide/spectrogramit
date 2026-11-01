namespace SpectrogramIt.Core.Model;

/// <summary>A single decoded audio buffer.</summary>
public sealed class AudioBuffer
{
    public float[] Samples { get; }
    public int SampleRate { get; }

    public AudioBuffer(float[] samples, int sampleRate)
    {
        Samples = samples;
        SampleRate = sampleRate;
    }

    public double DurationSeconds => (double)Samples.Length / SampleRate;
}

/// <summary>A single f0 measurement at a point in time.</summary>
public readonly struct PitchPoint
{
    /// <summary>Time of the frame center, in seconds.</summary>
    public double Time { get; }

    /// <summary>Fundamental frequency in Hz, or 0 when unvoiced.</summary>
    public double Frequency { get; }

    public PitchPoint(double time, double frequency)
    {
        Time = time;
        Frequency = frequency;
    }
}

/// <summary>One FFT frame of the spectrogram.</summary>
public readonly struct SpectrogramFrame
{
    /// <summary>Time of the frame center, in seconds.</summary>
    public double Time { get; }

    /// <summary>Magnitude spectrum. Index 0..(Length-1), length = fftSize/2 + 1.</summary>
    public double[] Magnitudes { get; }

    /// <summary>Frequency step between bins (Hz).</summary>
    public double BinWidth { get; }

    public SpectrogramFrame(double time, double[] magnitudes, double binWidth)
    {
        Time = time;
        Magnitudes = magnitudes;
        BinWidth = binWidth;
    }
}

/// <summary>Complete analysis of one audio buffer.</summary>
public sealed class AnalysisResult
{
    public required AudioBuffer Audio { get; init; }
    public required int AnalysisSampleRate { get; init; }
    public PitchPoint[] Pitch { get; init; } = Array.Empty<PitchPoint>();
    public SpectrogramFrame[] Spectrogram { get; init; } = Array.Empty<SpectrogramFrame>();
}