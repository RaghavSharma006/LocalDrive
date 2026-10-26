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
        if (Regex.IsMatch(text, @"\b(?:http|status(?:\s+code)?|response code)\D{0,10}401\b") || text.Contains("unauthorized") || text.Contains("unauthorised") || text.Contains("cookie") || text.Contains("login"))
            return "terabox-session-rejected";
        if (Regex.IsMatch(text, @"\b(?:http|status(?:\s+code)?|response code)\D{0,10}403\b") || text.Contains("forbidden"))
            return "terabox-forbidden";
        if (Regex.IsMatch(text, @"\b429\b") || text.Contains("too many requests"))
            return "terabox-throttled";
        if (Regex.IsMatch(text, @"timeout|timed out|connection refused|network is unreachable|no route to host|temporary failure|tls handshake"))
            return "terabox-network-error";
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

    internal static string SummarizeEngineFailure(string stderr)
    {
        // Persist only words from this fixed allowlist and standard OS error codes. Never
        // persist fragments of rclone output, which may contain paths or remote names.
        string text = stderr.ToLowerInvariant();
        string[] safeHints =
        [
            "fatal", "error", "failed", "failure", "mount", "fuse", "winfsp", "driver", "service", "launcher",
            "not", "found", "missing", "loaded", "load", "invalid", "parameter", "argument", "option",
            "access", "denied", "permission", "busy", "already", "exists", "create", "open", "start",
            "initialize", "network", "timeout", "connection", "refused", "unavailable", "system", "path",
            "cookie", "unauthorized", "forbidden", "throttled", "terabox", "401", "403", "429"
        ];
        var hints = safeHints.Where(hint => Regex.IsMatch(text, @"\b" + Regex.Escape(hint) + @"\b"));
        var codes = Regex.Matches(stderr, @"(?i)\b(?:win32|windows|system|error|status|hresult)\s*(?:error\s*)?(?:code\s*)?[:=]?\s*(0x[0-9a-f]{4,8}|[0-9]{2,5})\b")
            .Select(match => match.Groups[1].Value.ToUpperInvariant());
        string summary = string.Join(" ", hints.Concat(codes).Distinct(StringComparer.OrdinalIgnoreCase).Take(24));
        return summary.Length == 0 ? "No safe error hints available" : summary;
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

    internal static void RecordFailure(string letter, int exitCode, string category, string stderr)
    {
        Write("mount-failure.json", new
        {
            stage = "mount-engine-exited",
            driveLetter = letter,
            exitCode,
            errorCategory = category,
            errorSummary = SummarizeEngineFailure(stderr),
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
