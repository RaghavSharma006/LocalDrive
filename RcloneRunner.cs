using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LocalDrive;

internal sealed class RcloneCommandException(int exitCode, string diagnosticCategory) : IOException("rclone command failed")
{
    internal int ExitCode { get; } = exitCode;
    internal string DiagnosticCategory { get; } = diagnosticCategory;
    internal bool AuthenticationFailure => DiagnosticCategory is "terabox-session-rejected" or "terabox-forbidden";
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
            throw new RcloneCommandException(process.ExitCode, ClassifyProviderFailure(error));
        }
        try { using var _ = System.Text.Json.JsonDocument.Parse(output); }
        catch (System.Text.Json.JsonException) { throw new IOException("TeraBox returned an invalid folder listing."); }
    }

    private static string ClassifyProviderFailure(string error)
    {
        // rclone errors may include cookies, URLs and account data. Never persist or display
        // that text; reduce it to a small, fixed diagnostic vocabulary instead.
        if (Regex.IsMatch(error, @"\b(?:http|status(?:\s+code)?|response code)\D{0,10}401\b", RegexOptions.IgnoreCase)) return "terabox-session-rejected";
        if (Regex.IsMatch(error, @"\b(?:http|status(?:\s+code)?|response code)\D{0,10}403\b", RegexOptions.IgnoreCase)) return "terabox-forbidden";
        if (error.Contains("429", StringComparison.OrdinalIgnoreCase) || error.Contains("too many requests", StringComparison.OrdinalIgnoreCase)) return "terabox-throttled";
        if (error.Contains("cookie", StringComparison.OrdinalIgnoreCase) || error.Contains("unauthor", StringComparison.OrdinalIgnoreCase) || error.Contains("login", StringComparison.OrdinalIgnoreCase)) return "terabox-session-rejected";
        if (error.Contains("timeout", StringComparison.OrdinalIgnoreCase) || error.Contains("connection refused", StringComparison.OrdinalIgnoreCase) || error.Contains("network is unreachable", StringComparison.OrdinalIgnoreCase)) return "terabox-network-error";
        return "terabox-request-failed";
    }
}
