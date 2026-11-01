using SpectrogramIt.Controls;
using SpectrogramIt.Core;
using SpectrogramIt.Core.Model;

namespace SpectrogramIt.Views;

public partial class ViewerPage : ContentPage, IQueryAttributable
{
    private readonly AnalysisService _analysis;

    public ViewerPage(AnalysisService analysis)
    {
        InitializeComponent();
        _analysis = analysis;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("path", out var value) && value is string path)
            _ = LoadAsync(path);
    }

    private async System.Threading.Tasks.Task LoadAsync(string path)
    {
        Busy.IsVisible = true;
        Busy.IsRunning = true;
        FileNameLabel.Text = System.IO.Path.GetFileName(path);
        try
        {
            var result = await System.Threading.Tasks.Task.Run(() => _analysis.AnalyzeFile(path));
            Spectrogram.SetResult(result);
            InfoLabel.Text = $"时长 {result.Audio.DurationSeconds:F2}s · 采样率 {result.Audio.SampleRate}Hz · f0 帧 {result.Pitch.Length}";
        }
        catch (Exception ex)
        {
            InfoLabel.Text = $"加载失败：{ex.Message}";
        }
        finally
        {
            Busy.IsRunning = false;
            Busy.IsVisible = false;
        }
    }

    private void OnZoomOut(object? sender, EventArgs e) => Spectrogram.Zoom(1.3, 0.5);
    private void OnZoomIn(object? sender, EventArgs e) => Spectrogram.Zoom(1.0 / 1.3, 0.5);

    private void OnToggleScale(object? sender, EventArgs e)
    {
        Spectrogram.FrequencyAxis = Spectrogram.FrequencyAxis == FrequencyScale.Linear
            ? FrequencyScale.Log
            : FrequencyScale.Linear;
        ScaleBtn.Text = Spectrogram.FrequencyAxis == FrequencyScale.Linear ? "线性" : "对数";
    }

    private void OnSeekRequested(object? sender, double time)
    {
        InfoLabel.Text = $"t = {time:F3}s";
    }

    private void OnBack(object? sender, EventArgs e) =>
        Shell.Current?.GoToAsync("//Welcome");
}