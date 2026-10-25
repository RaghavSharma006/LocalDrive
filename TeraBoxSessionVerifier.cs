using System.IO;

namespace LocalDrive;

internal static class TeraBoxSessionVerifier
{
    internal static async Task VerifyAsync(TeraBoxSession session, CancellationToken cancellationToken)
    {
        string rclone = Path.Combine(Program.Engine, "tools", "rclone", "rclone.exe");
        if (!File.Exists(rclone)) throw new FileNotFoundException("The bundled TeraBox engine is missing.");
        string config = Path.Combine(Program.Home, "verify-" + Guid.NewGuid().ToString("N") + ".conf");
        TeraBoxSessionStore.WriteRcloneConfig(session, config);
        try { await RcloneRunner.VerifyRootAsync(rclone, config, cancellationToken); }
        finally { TeraBoxSessionStore.DeleteRcloneConfig(config); }
    }
}
