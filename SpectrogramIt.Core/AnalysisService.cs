using SpectrogramIt.Core.Analysis;
using SpectrogramIt.Core.Audio;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Core;

/// <summary>
/// Facade that loads an audio file and runs the MVP analysis pipeline:
/// decode → resample to the analysis rate → spectrogram + YIN pitch.
/// </summary>
public sealed class AnalysisService
{
    /// <summary>Sample rate used for spectrogram and pitch analysis.</summary>
    public const int AnalysisSampleRate = 44100;

    public AnalysisResult AnalyzeFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Analyze(stream);
    }

    public AnalysisResult Analyze(Stream stream)
    {
        var buffer = WaveReader.Read(stream);
        var resampler = new AudioResampler(buffer);
        var audio = resampler.GetBuffer(AnalysisSampleRate);

        var spectrogram = new SpectrogramBuilder().Build(audio.Samples, AnalysisSampleRate);
        var pitch = new YinPitchTracker().Track(audio.Samples, AnalysisSampleRate);

        return new AnalysisResult
        {
            Audio = audio,
            AnalysisSampleRate = AnalysisSampleRate,
            Pitch = pitch,
            Spectrogram = spectrogram,
        };
    }
}