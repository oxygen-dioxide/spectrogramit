using System.Buffers.Binary;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core.Audio;

/// <summary>Minimal RIFF/WAVE PCM reader supporting 16-bit PCM and 32-bit float.</summary>
public static class WaveReader
{
    public static AudioBuffer Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        if (new string(reader.ReadChars(4)) != "RIFF")
            throw new FormatException("Not a RIFF file.");

        _ = reader.ReadUInt32(); // riff size
        if (new string(reader.ReadChars(4)) != "WAVE")
            throw new FormatException("Not a WAVE file.");

        int channels = 0;
        int sampleRate = 0;
        int bitsPerSample = 0;
        int formatTag = 0;
        long dataOffset = 0;
        long dataLength = 0;
        var data = new List<float>();

        // Read chunks until we hit data.
        while (stream.Position < stream.Length - 8)
        {
            var id = new string(reader.ReadChars(4));
            var size = reader.ReadUInt32();

            switch (id)
            {
                case "fmt ":
                {
                    var fmtSize = size;
                    formatTag = reader.ReadUInt16();
                    channels = reader.ReadUInt16();
                    sampleRate = reader.ReadInt32();
                    var byteRate = reader.ReadInt32();
                    var blockAlign = reader.ReadUInt16();
                    bitsPerSample = reader.ReadUInt16();
                    // skip remainder of fmt chunk
                    stream.Position += fmtSize - 16;
                    _ = byteRate; _ = blockAlign;
                    break;
                }
                case "data":
                    dataOffset = stream.Position;
                    dataLength = size;
                    // read the samples
                    if (formatTag == 1 && bitsPerSample == 16)
                    {
                        var count = dataLength / 2;
                        data.Capacity = (int)count;
                        for (long i = 0; i < count; i++)
                            data.Add(reader.ReadInt16() / 32768f);
                    }
                    else if (formatTag == 3 && bitsPerSample == 32)
                    {
                        var count = dataLength / 4;
                        data.Capacity = (int)count;
                        for (long i = 0; i < count; i++)
                            data.Add(reader.ReadSingle());
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Unsupported WAV format (tag={formatTag}, bits={bitsPerSample}).");
                    }

                    // The data chunk is (possibly) followed by a padding byte.
                    if (stream.Position < stream.Length && stream.Length - stream.Position > 0 && dataLength % 2 != 0)
                        stream.Position += 1;

                    // If the file ends here we're done.
                    return BuildAudioBuffer(data, channels, sampleRate);
                default:
                    // Skip unknown chunk.
                    stream.Position += size + (size % 2);
                    break;
            }
        }

        _ = dataOffset; _ = dataLength;
        throw new FormatException("No data chunk found.");
    }

    private static AudioBuffer BuildAudioBuffer(List<float> data, int channels, int sampleRate)
    {
        if (channels == 1)
            return new AudioBuffer(data.ToArray(), sampleRate);

        // Downmix multi-channel to mono (average).
        var mono = new float[data.Count / channels];
        for (int i = 0; i < mono.Length; i++)
        {
            float sum = 0;
            for (int c = 0; c < channels; c++)
                sum += data[i * channels + c];
            mono[i] = sum / channels;
        }
        return new AudioBuffer(mono, sampleRate);
    }
}