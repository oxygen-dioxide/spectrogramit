using NWaves.Operations;
using NWaves.Signals;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core.Audio;

/// <summary>Resamples audio between sample rates. Results are cached per target rate.</summary>
public sealed class AudioResampler
{
    private readonly AudioBuffer _source;
    private readonly Dictionary<int, float[]> _cache = new();

    public AudioBuffer Source => _source;

    public AudioResampler(AudioBuffer source)
    {
        _source = source;
        _cache[_source.SampleRate] = _source.Samples;
    }

    /// <summary>Returns samples at the requested sample rate (cached).</summary>
    public float[] GetSamples(int sampleRate)
    {
        if (_cache.TryGetValue(sampleRate, out var cached))
            return cached;

        var resampler = new Resampler();
        var signal = new DiscreteSignal(_source.SampleRate, _source.Samples);
        var output = resampler.Resample(signal, sampleRate);
        var result = output.Samples;
        _cache[sampleRate] = result;
        return result;
    }

    /// <summary>Returns an AudioBuffer at the requested sample rate (cached).</summary>
    public AudioBuffer GetBuffer(int sampleRate) =>
        new AudioBuffer(GetSamples(sampleRate), sampleRate);
}