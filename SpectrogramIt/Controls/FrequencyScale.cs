namespace SpectrogramIt.Controls;

/// <summary>Frequency axis mapping modes for the spectrogram view.</summary>
public enum FrequencyScale
{
    Linear,
    Log,
}

/// <summary>Maps frequency in Hz to a normalized [0,1] vertical position for a given scale.</summary>
public static class FrequencyScaleMapper
{
    public const double MinDisplayHz = 40.0;

    /// <summary>Returns a normalized position in [0,1] for the given frequency and scale.</summary>
    public static double FreqToNorm(double hz, FrequencyScale scale, double nyquist)
    {
        switch (scale)
        {
            case FrequencyScale.Log:
                if (hz <= MinDisplayHz)
                    return 0.0;
                double logLow = Math.Log10(MinDisplayHz);
                double logHigh = Math.Log10(nyquist);
                return Math.Clamp((Math.Log10(hz) - logLow) / (logHigh - logLow), 0.0, 1.0);
            default:
                return Math.Clamp(hz / nyquist, 0.0, 1.0);
        }
    }

    /// <summary>Returns the frequency in Hz at a normalized [0,1] position, inverse of <see cref="FreqToNorm"/>.</summary>
    public static double NormToFreq(double norm, FrequencyScale scale, double nyquist)
    {
        switch (scale)
        {
            case FrequencyScale.Log:
                double logLow = Math.Log10(MinDisplayHz);
                double logHigh = Math.Log10(nyquist);
                double f = Math.Pow(10, logLow + norm * (logHigh - logLow));
                return Math.Clamp(f, MinDisplayHz, nyquist);
            default:
                return Math.Clamp(norm * nyquist, 0.0, nyquist);
        }
    }
}