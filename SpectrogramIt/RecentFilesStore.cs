using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;

namespace SpectrogramIt;

/// <summary>Persists a short list of recently opened audio file paths.</summary>
public sealed class RecentFilesStore
{
    private const string Key = "recent_files";
    private const int Max = 20;

    public IReadOnlyList<string> Load()
    {
        var raw = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return System.Array.Empty<string>();

        var list = raw
            .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Where(p => File.Exists(p) || p.StartsWith("/") || p.Contains("://"))
            .ToList();
        return list;
    }

    public void Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var list = new List<string>(Load());
        list.RemoveAll(p => string.Equals(p, path, System.StringComparison.OrdinalIgnoreCase));
        list.Insert(0, path);
        if (list.Count > Max)
            list.RemoveRange(Max, list.Count - Max);

        Preferences.Default.Set(Key, string.Join(';', list));
    }
}