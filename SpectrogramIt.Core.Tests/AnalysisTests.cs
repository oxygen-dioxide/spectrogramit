using System.IO;
using SpectrogramIt.Core;
using SpectrogramIt.Core.Analysis;

namespace SpectrogramIt.Core.Tests;

public class AnalysisTests
{
    private static string TempWav(string name, int sampleRate, float[] samples)
    {
        var dir = Path.Combine(Path.GetTempPath(), "spectrogramit-tests");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        WavWriter.Write(path, sampleRate, samples);
        return path;
    }

    [Fact]
    public void Yin_DetectsSineWave_F0()
    {
        const int sampleRate = 44100;
        const double f0 = 200.0;
        const double seconds = 0.6;
        var samples = new float[(int)(sampleRate * seconds)];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)Math.Sin(2 * Math.PI * f0 * i / sampleRate);

        var path = TempWav("yin_sine_200hz.wav", sampleRate, samples);
        var service = new AnalysisService();
        var result = service.AnalyzeFile(path);

        Assert.NotEmpty(result.Pitch);
        double sum = 0;
        int count = 0;
        foreach (var p in result.Pitch)
        {
            if (p.Frequency > 0)
            {
                sum += p.Frequency;
                count++;
            }
        }
        Assert.True(count > 10, $"Expected many voiced frames, got {count}");
        var avg = sum / count;
        Assert.InRange(avg, 190, 210);
    }
}