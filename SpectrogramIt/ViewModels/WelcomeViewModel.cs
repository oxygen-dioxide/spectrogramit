using System.Collections.ObjectModel;
using System.IO;

namespace SpectrogramIt.ViewModels;

/// <summary>An item in the recent-files list.</summary>
public sealed class RecentFileItem
{
    public required string Path { get; init; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
    public override string ToString() => Path;
}

public sealed class WelcomeViewModel : ObservableObject
{
    private readonly RecentFilesStore _store;

    public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();
    public RelayCommand OpenCommand { get; }
    public RelayCommand<RecentFileItem> OpenRecentCommand { get; }
    public RelayCommand GotoSettingsCommand { get; }

    private bool _hasRecent;
    public bool HasRecent { get => _hasRecent; set => SetProperty(ref _hasRecent, value); }
    public bool NoRecent => !HasRecent;

    public WelcomeViewModel(RecentFilesStore store)
    {
        _store = store;
        OpenCommand = new RelayCommand(async () => await OpenPickerAsync());
        OpenRecentCommand = new RelayCommand<RecentFileItem>(item =>
        {
            if (item != null) OpenFile(item.Path);
        });
        GotoSettingsCommand = new RelayCommand(() =>
            Shell.Current?.GoToAsync("//Settings"));
        RefreshRecent();
    }

    public void RefreshRecent()
    {
        RecentFiles.Clear();
        foreach (var p in _store.Load())
            RecentFiles.Add(new RecentFileItem { Path = p });
        HasRecent = RecentFiles.Count > 0;
        OnPropertyChanged(nameof(NoRecent));
    }

    private async System.Threading.Tasks.Task OpenPickerAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择音频文件",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".wav" } },
                    { DevicePlatform.Android, new[] { "audio/wav", "audio/x-wav", "audio/pcm" } },
                    { DevicePlatform.iOS, new[] { "public.audio" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.audio" } },
                }),
            });

            if (result != null)
                OpenFile(result.FullPath);
        }
        catch
        {
            // Picker dismissed or unsupported on this platform.
        }
    }

    private void OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        _store.Add(path);
        RefreshRecent();
        Shell.Current?.GoToAsync("//Viewer", new Dictionary<string, object>
        {
            ["path"] = path,
        });
    }
}