using System.IO;

namespace LocalDrive;

internal static class ConnectionProbe
{
    // Uses only the encrypted session already saved by LocalDrive and never writes it to output.
    internal static async Task<int> RunAsync()
    {
        var store = new TeraBoxSessionStore(Path.Combine(Program.Home, "terabox.session.dpapi"));
        var session = store.Load();
        if (session == null) { Console.WriteLine("No saved app session"); return 1; }
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            await TeraBoxSessionVerifier.VerifyAsync(session, timeout.Token);
            Console.WriteLine("TeraBox root listing: OK");
            return 0;
        }
        catch (RcloneCommandException e)
        {
            Console.WriteLine(e.AuthenticationFailure ? "TeraBox rejected the saved sign-in" : "TeraBox listing failed (rclone exit " + e.ExitCode + ")");
            return 1;
        }
        catch (Exception e)
        {
            Console.WriteLine("TeraBox check failed (" + e.GetType().Name + ")");
            return 1;
        }
    }
}
