using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using WpfApplication = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace LocalDrive;

internal static class Program
{
    internal const uint RestoreWindowMessage = 0x8001;
    // The community rclone-extra engine is placed beside LocalDrive.exe by the setup builder.
    internal static readonly string Engine = Path.Combine(AppContext.BaseDirectory, "Engine");
    internal static readonly string Home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalDrive");

    [STAThread]
    public static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception e)
        {
            // Do not print exception messages that might contain signed URLs or authentication data.
            MessageBox.Show("LocalDrive could not start. Check that the TeraBox engine and WinFsp are installed. Error: " + e.GetType().Name, "LocalDrive");
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args)
    {
        Directory.CreateDirectory(Home);
        if (args.Length == 2 && args[0] == "--diagnose-executable")
        {
            var probe = ExecutableProbeRunner.Probe(args[1]);
            MountDiagnostics.RecordExecutableProbe(Path.GetExtension(args[1]), probe);
            Console.WriteLine($"Read-open: {(probe.ReadOpenSucceeded ? "PASS" : "FAIL " + probe.ReadOpenError)}; SEC_IMAGE section: {(probe.ImageSectionSucceeded ? "PASS" : "FAIL " + probe.ImageSectionError)}");
            return probe.ReadOpenSucceeded && probe.ImageSectionSucceeded ? 0 : 1;
        }
        if (args.Contains("--verify-file-ops")) return BasicFileOperationTest.Run();
        if (args.Contains("--diagnose-connection")) return ConnectionProbe.RunAsync().GetAwaiter().GetResult();
        bool preview = args.Contains("--preview-ui");
        string mutexName = "Local\\LocalDrive-" + Environment.UserName + (preview ? "-Preview" : "");
        using var single = new Mutex(true, mutexName, out bool first);
        if (!first)
        {
            // A second launch should bring the existing tray app back, not leave the
            // user with an “already running” dialog and no obvious way forward.
            var window = FindWindow(null, "LocalDrive");
            if (window == IntPtr.Zero || !PostMessage(window, RestoreWindowMessage, IntPtr.Zero, IntPtr.Zero))
                MessageBox.Show("LocalDrive is starting. Try opening it again in a moment.", "LocalDrive");
            return 0;
        }
        var app = new WpfApplication();
        return app.Run(new DriveWindow(args.Contains("--connect-saved"), preview));
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
