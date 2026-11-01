using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Controls;

/// <summary>
/// Interactive spectrogram view: owns the viewport (xStart/xSpan seconds),
/// handles pinch/pan/wheel/tap gestures, and paints the pre-rendered
/// spectrogram plus the f0 overlay.
/// </summary>
public sealed class SpectrogramView : ContentView
{
    private readonly SKCanvasView _canvas;
    private SpectrogramRenderer? _renderer;

    private double _xStart;
    private double _xSpan = 1.0;
    private FrequencyScale _scale = FrequencyScale.Linear;

    private SKPoint _lastTouch;
    private bool _dragging;
    private bool _moved;

    public event EventHandler<double>? SeekRequested;

    public SpectrogramView()
    {
        _canvas = new SKCanvasView { EnableTouchEvents = true };
        _canvas.PaintSurface += OnPaintSurface;
        _canvas.Touch += OnTouch;

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinch);

        Content = _canvas;
    }

    public void SetResult(AnalysisResult result)
    {
        _renderer = new SpectrogramRenderer(result);
        _xStart = 0;
        _xSpan = result.Audio.DurationSeconds;
        _canvas.InvalidateSurface();
    }

    public FrequencyScale FrequencyAxis
    {
        get => _scale;
        set
        {
            if (_scale == value) return;
            _scale = value;
            _canvas.InvalidateSurface();
        }
    }

    /// <summary>Multiplies the horizontal span by <paramref name="mult"/>, keeping <paramref name="center"/> fixed.</summary>
    public void Zoom(double mult, double center)
    {
        if (_renderer == null) return;
        double duration = Math.Max(_renderer.DurationSeconds, 0.1);
        double xCenter = _xStart + _xSpan * Math.Clamp(center, 0, 1);
        _xSpan = Math.Clamp(_xSpan * mult, 0.01, duration);
        _xStart = xCenter - _xSpan * Math.Clamp(center, 0, 1);
        _xStart = Math.Clamp(_xStart, 0, Math.Max(0, duration - _xSpan));
        _canvas.InvalidateSurface();
    }

    public void PanByPixels(double deltaPixels)
    {
        if (_renderer == null) return;
        double delta = deltaPixels * _xSpan / Math.Max(_canvas.Width, 1);
        _xStart += delta;
        double duration = Math.Max(_renderer.DurationSeconds, 0.1);
        _xStart = Math.Clamp(_xStart, 0, Math.Max(0, duration - _xSpan));
        _canvas.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_renderer == null) return;
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);
        var dest = new SKRect(0, 0, e.Info.Width, e.Info.Height);
        _renderer.Draw(canvas, dest, _xStart, _xSpan, _scale);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _lastTouch = e.Location;
                _dragging = true;
                _moved = false;
                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (_dragging)
                {
                    double dx = e.Location.X - _lastTouch.X;
                    if (Math.Abs(dx) > 1) _moved = true;
                    PanByPixels(dx);
                    _lastTouch = e.Location;
                }
                e.Handled = true;
                break;

            case SKTouchAction.Released:
                if (_dragging && !_moved)
                {
                    // Tap → seek.
                    double t = _xStart + e.Location.X / Math.Max(_canvas.Width, 1) * _xSpan;
                    SeekRequested?.Invoke(this, t);
                }
                _dragging = false;
                e.Handled = true;
                break;

            case SKTouchAction.WheelChanged:
                double delta = e.WheelDelta;
                double mult = delta > 0 ? 1.0 / 1.3 : 1.3;
                double center = e.Location.X / Math.Max(_canvas.Width, 1);
                Zoom(mult, center);
                e.Handled = true;
                break;
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartX = _xStart;
                _pinchStartSpan = _xSpan;
                break;

            case GestureStatus.Running:
                if (e.Scale > 0)
                {
                    double center = e.ScaleOrigin.X / Math.Max(_canvas.Width, 1);
                    double duration = Math.Max(_renderer?.DurationSeconds ?? 0.1, 0.1);
                    double tAtOrigin = _pinchStartX + _pinchStartSpan * Math.Clamp(center, 0, 1);
                    _xSpan = Math.Clamp(_pinchStartSpan / e.Scale, 0.01, duration);
                    _xStart = tAtOrigin - _xSpan * Math.Clamp(center, 0, 1);
                    _xStart = Math.Clamp(_xStart, 0, Math.Max(0, duration - _xSpan));
                    _canvas.InvalidateSurface();
                }
                break;
        }
    }

    private double _pinchStartX;
    private double _pinchStartSpan;
}