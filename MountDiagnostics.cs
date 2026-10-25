using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace LocalDrive;

internal static class MountDiagnostics
{
    internal static string ClassifyEngineFailure(string stderr)
    {
        // rclone stderr can contain provider URLs, remote file names and account details.
        // Inspect it in memory only, and persist one of these fixed, non-sensitive labels.
        string text = stderr.ToLowerInvariant();
        if (Regex.IsMatch(text, @"winfsp.{0,100}(not installed|not found|unavailable|could not|failed)|(?:not installed|not found|unavailable).{0,100}winfsp"))
            return "winfsp-unavailable";
        if (Regex.IsMatch(text, @"(drive|mount.?point|volume).{0,100}(already in use|already exists|busy|occupied)|(?:already in use|already exists|busy|occupied).{0,100}(drive|mount.?point|volume)|object_name_collision|device_already_attached"))
            return "drive-letter-in-use";
        if (text.Contains("access is denied") || text.Contains("access denied") || text.Contains("permission denied") || text.Contains("status_access_denied"))
            return "mount-access-denied";
        if (Regex.IsMatch(text, @"unknown (flag|option)|invalid (argument|option)|flag provided but not defined|unrecognized option"))
            return "invalid-mount-options";
        if (Regex.IsMatch(text, @"timeout|timed out|connection refused|network is unreachable|no route to host|temporary failure|tls handshake"))
            return "mount-transport-error";
        if (text.Contains("winfsp")) return "winfsp-mount-error";
        return "unclassified-mount-error";
    }

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

    internal static void RecordFailure(string letter, int exitCode, string category)
    {
        Write("mount-failure.json", new
        {
            stage = "mount-engine-exited",
            driveLetter = letter,
            exitCode,
            errorCategory = category,
            filePermissions = MountConfiguration.FilePermissions,
            directoryPermissions = MountConfiguration.DirectoryPermissions,
            vfsCacheMode = "full",
            winfspLauncherPresent = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinFsp", "bin", "launcher-x64.exe")),
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
