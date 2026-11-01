using SkiaSharp;
using SpectrogramIt.Core.Model;
using SpectrogramIt.Core;

namespace SpectrogramIt.Controls;

/// <summary>
/// Pre-renders a spectrogram from analysis results and draws it (with a pitch
/// overlay) into a viewport. Follows the "pre-render + source-slice + vector
/// overlay" pattern from OpenUtau's OtoPlot.
/// </summary>
public sealed class SpectrogramRenderer
{
    private readonly AnalysisResult _result;
    private readonly Dictionary<FrequencyScale, SKBitmap> _cache = new();
    private double _dbFloor = -80.0;
    private int _maxColumns = 4096;
    private int _outputBins = 640;

    public SpectrogramRenderer(AnalysisResult result)
    {
        _result = result;
    }

    public double Nyquist => _result.AnalysisSampleRate / 2.0;
    public double DurationSeconds => _result.Audio.DurationSeconds;

    /// <summary>Gets (or builds) the pre-rendered spectrogram bitmap for a scale.</summary>
    public SKBitmap GetBitmap(FrequencyScale scale)
    {
        if (_cache.TryGetValue(scale, out var cached))
            return cached;

        var frames = _result.Spectrogram;
        if (frames.Length == 0)
        {
            var empty = new SKBitmap(1, 1);
            _cache[scale] = empty;
            return empty;
        }

        double binWidth = frames[0].BinWidth;
        double nyquist = Nyquist;

        // Columns from frames, capped to bound memory; time-average when capped.
        int columns = Math.Min(frames.Length, _maxColumns);
        int rows = _outputBins;

        // Global normalization from the maximum magnitude across all frames.
        double globalMax = 1.0;
        foreach (var f in frames)
        {
            for (int k = 0; k < f.Magnitudes.Length; k++)
            {
                if (f.Magnitudes[k] > globalMax)
                    globalMax = f.Magnitudes[k];
            }
        }
        globalMax = Math.Max(globalMax, 1e-12);

        var bmp = new SKBitmap(columns, rows);
        using (var canvas = new SKCanvas(bmp))
        {
            canvas.Clear(SKColors.Black);
            using var paint = new SKPaint { IsAntialias = false };
            var pixel = new SKRect();

            for (int x = 0; x < columns; x++)
            {
                int frameIdx = x * frames.Length / columns;
                var frame = frames[frameIdx];

                for (int y = 0; y < rows; y++)
                {
                    double norm = 1.0 - (double)y / (rows - 1);
                    double freq = FrequencyScaleMapper.NormToFreq(norm, scale, nyquist);
                    int bin = (int)Math.Round(freq / binWidth);
                    bin = Math.Clamp(bin, 0, frame.Magnitudes.Length - 1);

                    double mag = frame.Magnitudes[bin];
                    double db = mag > 1e-9 ? 20.0 * Math.Log10(mag / globalMax) : _dbFloor;
                    double t = Math.Clamp((db - _dbFloor) / (0.0 - _dbFloor), 0.0, 1.0);

                    paint.Color = ColorMap(t);
                    pixel.Left = x; pixel.Top = y; pixel.Right = x + 1; pixel.Bottom = y + 1;
                    canvas.DrawRect(pixel, paint);
                }
            }
        }

        _cache[scale] = bmp;
        return bmp;
    }

    /// <summary>Draws the visible portion of the spectrogram and pitch overlay into the canvas.</summary>
    public void Draw(SKCanvas canvas, SKRect dest, double xStart, double xSpan, FrequencyScale scale)
    {
        var bmp = GetBitmap(scale);
        double duration = Math.Max(DurationSeconds, 1e-6);
        double clampedXStart = Math.Clamp(xStart, 0.0, Math.Max(0, duration - xSpan));
        double xEnd = Math.Min(clampedXStart + xSpan, duration);

        double srcLeft = clampedXStart / duration * bmp.Width;
        double srcRight = xEnd / duration * bmp.Width;
        var srcRect = SKRect.Create((float)srcLeft, 0, (float)(srcRight - srcLeft), bmp.Height);

        canvas.DrawBitmap(bmp, srcRect, dest, new SKSamplingOptions(SKCubicResampler.Mitchell));
        DrawPitch(canvas, dest, clampedXStart, xSpan, scale);
    }

    private void DrawPitch(SKCanvas canvas, SKRect dest, double xStart, double xSpan, FrequencyScale scale)
    {
        var pitch = _result.Pitch;
        if (pitch.Length == 0)
            return;

        var builder = new SKPathBuilder();
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.White,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, 0),
        };

        bool started = false;
        foreach (var p in pitch)
        {
            if (p.Frequency <= 0)
            {
                started = false;
                continue;
            }
            double x = (p.Time - xStart) / xSpan * dest.Width;
            if (x < -1 || x > dest.Width + 1)
            {
                started = false;
                continue;
            }
            double y = (1.0 - FrequencyScaleMapper.FreqToNorm(p.Frequency, scale, Nyquist)) * dest.Height;
            if (!started)
            {
                builder.MoveTo((float)x, (float)y);
                started = true;
            }
            else
            {
                builder.LineTo((float)x, (float)y);
            }
        }
        canvas.DrawPath(builder.Detach(), paint);
    }

    private static SKColor ColorMap(double t)
    {
        // A compact blue→purple→red→yellow colormap approximating magma.
        float r = (float)Math.Clamp(0.0 + 2.5 * t - 2.2 * Math.Max(0, t - 0.55) * (t - 0.55) * 4, 0, 1);
        float g = (float)Math.Clamp(0.25 * t, 0, 1);
        float b = (float)Math.Clamp(1.0 - t, 0, 1);
        return new SKColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}