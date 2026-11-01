using System.IO;
using System.Text;

namespace SpectrogramIt.Core.Tests;

/// <summary>Writes 16-bit PCM mono WAV files for tests.</summary>
internal static class WavWriter
{
    public static void Write(string path, int sampleRate, float[] samples)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs, Encoding.ASCII);

        var dataSize = samples.Length * 2;
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);           // PCM
        bw.Write((short)1);           // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);     // byte rate
        bw.Write((short)2);           // block align
        bw.Write((short)16);          // bits per sample

        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        foreach (var s in samples)
        {
            short pcm = (short)Math.Clamp((int)(s * 32767), short.MinValue, short.MaxValue);
            bw.Write(pcm);
        }
    }
}