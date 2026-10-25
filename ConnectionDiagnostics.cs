using System.IO;
using System.Text.Json;

namespace LocalDrive;

internal static class ConnectionDiagnostics
{
    // Only fixed stage identifiers and exception kinds/provider codes; never cookies, URLs or user files.
    internal static void Record(string stage, string? errorKind = null, string? failedStep = null)
    {
        try
        {
            Directory.CreateDirectory(Program.Home);
            string path = Path.Combine(Program.Home, "connection-status.json");
            string pending = path + ".tmp";
            File.WriteAllText(pending, JsonSerializer.Serialize(new { stage, errorKind, failedStep, timestampUtc = DateTimeOffset.UtcNow }));
            File.Move(pending, path, true);
        }
        catch { /* Diagnostic storage must not interrupt account or filesystem operations. */ }
    }
}
