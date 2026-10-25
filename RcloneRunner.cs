using System.Diagnostics;
using System.IO;

namespace LocalDrive;

internal sealed class RcloneCommandException(int exitCode, bool authenticationFailure) : IOException("rclone command failed")
{
    internal int ExitCode { get; } = exitCode;
    internal bool AuthenticationFailure { get; } = authenticationFailure;
}

internal static class RcloneRunner
{
    internal static async Task VerifyRootAsync(string executable, string configPath, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("lsjson");
        start.ArgumentList.Add("localdrive:");
        start.ArgumentList.Add("--max-depth");
        start.ArgumentList.Add("1");
        start.ArgumentList.Add("--config");
        start.ArgumentList.Add(configPath);
        using var process = Process.Start(start) ?? throw new IOException("Could not start the TeraBox engine.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        string output = await stdout;
        string error = await stderr;
        if (process.ExitCode != 0)
        {
            bool auth = error.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                        error.Contains("403", StringComparison.OrdinalIgnoreCase) ||
                        error.Contains("unauthor", StringComparison.OrdinalIgnoreCase) ||
                        error.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
                        error.Contains("login", StringComparison.OrdinalIgnoreCase);
            throw new RcloneCommandException(process.ExitCode, auth);
        }
        try { using var _ = System.Text.Json.JsonDocument.Parse(output); }
        catch (System.Text.Json.JsonException) { throw new IOException("TeraBox returned an invalid folder listing."); }
    }
}
