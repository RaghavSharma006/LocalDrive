using System.IO;
using System.Text.Json;

namespace LocalDrive;

internal static class CacheSettings
{
    private sealed class Settings
    {
        public string CacheDirectory { get; set; } = DefaultDirectory;
        public int CacheSizeGb { get; set; } = 2;
        public bool DarkTheme { get; set; }
    }

    private static string SettingsPath => Path.Combine(Program.Home, "settings.json");
    internal static string DefaultDirectory => Path.Combine(Program.Home, "cache");

    private static Settings Read()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    internal static string Load() => Path.GetFullPath(Read().CacheDirectory);
    internal static int LoadSizeGb() => Math.Clamp(Read().CacheSizeGb, 1, 4096);
    internal static bool LoadDarkTheme() => Read().DarkTheme;

    internal static void SaveDirectory(string directory) => Update(s => s.CacheDirectory = Path.GetFullPath(directory));
    internal static void SaveSizeGb(int size) => Update(s => s.CacheSizeGb = Math.Clamp(size, 1, 4096));
    internal static void SaveTheme(bool dark) => Update(s => s.DarkTheme = dark);

    private static void Update(Action<Settings> update)
    {
        Directory.CreateDirectory(Program.Home);
        var settings = Read();
        update(settings);
        string temporary = SettingsPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, SettingsPath, true);
    }

    internal static string ValidateAndPrepare(string requested, string mountLetter)
    {
        string full = Path.GetFullPath(requested);
        string? cacheRoot = Path.GetPathRoot(full);
        string? mountRoot = Path.GetPathRoot(mountLetter + "\\");
        if (cacheRoot != null && mountRoot != null && string.Equals(cacheRoot, mountRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a cache folder on a different drive from the TeraBox mount.");

        string probe = Path.Combine(full, ".localdrive-write-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(full);
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new IOException("The selected cache folder is not writable. Choose an available drive and folder.", e);
        }
        finally
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { }
        }
        return full;
    }
}
