using System.Text.Json;

namespace LocalDrive;

internal readonly record struct TransferState(int Queued, int Uploading, int Errors, bool OutOfSpace)
{
    internal bool SafeToDisconnect => Queued == 0 && Uploading == 0 && Errors == 0 && !OutOfSpace;
    internal string Description => OutOfSpace ? "Local cache is full. Free disk space; keep this app open."
        : Errors > 0 ? $"{Errors} file(s) could not upload. Changes remain in the local cache; keep this app open for retries."
        : Queued + Uploading > 0 ? $"Uploading: {Uploading} • Waiting: {Queued}. Keep LocalDrive open."
        : "No queued uploads. Close any edited files before disconnecting.";

    internal static TransferState Parse(JsonElement root)
    {
        var cache = root.GetProperty("diskCache");
        return new(cache.GetProperty("uploadsQueued").GetInt32(), cache.GetProperty("uploadsInProgress").GetInt32(),
            cache.GetProperty("erroredFiles").GetInt32(), cache.GetProperty("outOfSpace").GetBoolean());
    }
}
