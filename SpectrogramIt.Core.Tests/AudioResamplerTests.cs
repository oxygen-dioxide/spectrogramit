using SpectrogramIt.Core.Audio;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core.Tests;

public class AudioResamplerTests
{
    [Fact]
    public void Resample_ChangesLength_Proportionally()
    {
        const int srcRate = 44100;
        const int dstRate = 22050;
        var samples = new float[44100];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)Math.Sin(2 * Math.PI * 440 * i / srcRate);

        var buffer = new AudioBuffer(samples, srcRate);
        var resampler = new AudioResampler(buffer);
        var outSamples = resampler.GetSamples(dstRate);

        // Length ratio should be approximately dstRate/srcRate = 0.5.
        double ratio = (double)outSamples.Length / samples.Length;
        Assert.InRange(ratio, 0.48, 0.52);
    }

    [Fact]
    public void Resample_Cache_ReturnsSameInstance()
    {
        var samples = new float[1000];
        var buffer = new AudioBuffer(samples, 44100);
        var resampler = new AudioResampler(buffer);

        var first = resampler.GetSamples(22050);
        var second = resampler.GetSamples(22050);
        Assert.Same(first, second);
    }
}