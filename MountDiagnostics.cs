using System.IO;
using System.Text.Json;

namespace LocalDrive;

internal static class MountDiagnostics
{
    internal static void RecordMount(string letter, int processId, string cacheDirectory, int cacheSizeGb)
    {
        Write("mount.json", new
        {
            stage = "mount-started",
            driveLetter = letter,
            processId,
            cacheDrive = Path.GetPathRoot(cacheDirectory),
            cacheSizeGb,
            filePermissions = MountConfiguration.FilePermissions,
            directoryPermissions = MountConfiguration.DirectoryPermissions,
            vfsCacheMode = "full",
            timestampUtc = DateTimeOffset.UtcNow
        });
    }

    internal static void RecordExecutableProbe(string extension, ExecutableProbe result)
    {
        Write("executable-access.json", new
        {
            extension,
            result.ReadOpenSucceeded,
            result.ReadOpenError,
            result.ImageSectionSucceeded,
            result.ImageSectionError,
            timestampUtc = DateTimeOffset.UtcNow
        });
    }

    private static void Write(string name, object value)
    {
        try
        {
            Directory.CreateDirectory(Program.Home);
            File.WriteAllText(Path.Combine(Program.Home, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
