using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace LocalDrive;

internal sealed class DriveWindow : Window
{
    private readonly ComboBox letters = new() { Width = 100, Margin = new Thickness(0, 0, 16, 0) };
    private readonly TextBlock status = new() { Text = "Sign in to connect your entire TeraBox account.", TextWrapping = TextWrapping.Wrap };
    private readonly Button login = new() { Content = "Sign in to TeraBox", Width = 150 };
    private readonly Button mount = new() { Content = "Connect drive", Width = 125 };
    private readonly Button unmount = new() { Content = "Disconnect", Width = 110, IsEnabled = false };
    private readonly Button open = new() { Content = "Open drive", Width = 110, IsEnabled = false };
    private readonly Button chooseCache = new() { Content = "Choose cache folder…", Padding = new Thickness(8) };
    private readonly TextBox cacheSize = new() { Width = 58, Text = CacheSettings.LoadSizeGb().ToString(), VerticalContentAlignment = VerticalAlignment.Center };
    private readonly Button saveCacheSize = new() { Content = "Save size", Padding = new Thickness(8) };
    private readonly Button openCache = new() { Content = "Open cache", Padding = new Thickness(8) };
    private readonly Button themeToggle = new() { Content = "Dark mode", Width = 100, Padding = new Thickness(8) };
    private readonly TextBlock cacheLocation = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock transfers = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private readonly System.Windows.Threading.DispatcherTimer transferTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private bool checkingTransfers;
    private Process? process;
    private HttpClient? rc;
    private string? drive;
    private bool busy, closing;
    private bool darkTheme;
    private readonly System.Windows.Forms.NotifyIcon trayIcon = new() { Text = "LocalDrive", Icon = System.Drawing.SystemIcons.Application };
    private readonly TeraBoxSessionStore store = new(Path.Combine(Program.Home, "terabox.session.dpapi"));
    private readonly string mountConfigPath = Path.Combine(Program.Home, "rclone-mount.conf");

    public DriveWindow(bool connectOnStart = false)
    {
        Title = "LocalDrive"; Width = 900; Height = 700; MinWidth = 760; MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(246, 248, 251));
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var panel = new StackPanel { Margin = new Thickness(30), Orientation = System.Windows.Controls.Orientation.Vertical };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        var heading = new Grid { Margin = new Thickness(0, 0, 0, 26) };
        heading.ColumnDefinitions.Add(new ColumnDefinition());
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var identity = new StackPanel();
        identity.Children.Add(new TextBlock { Text = "LocalDrive", FontSize = 30, FontWeight = FontWeights.SemiBold });
        identity.Children.Add(new TextBlock { Text = "Your TeraBox files, available as a Windows drive.", Margin = new Thickness(0, 4, 0, 0), Foreground = Brushes.SlateGray });
        heading.Children.Add(identity);
        Grid.SetColumn(themeToggle, 1); themeToggle.VerticalAlignment = VerticalAlignment.Top; heading.Children.Add(themeToggle);
        panel.Children.Add(heading);

        var driveCard = new StackPanel { Margin = new Thickness(20) };
        driveCard.Children.Add(new TextBlock { Text = "Cloud drive", FontSize = 19, FontWeight = FontWeights.SemiBold });
        driveCard.Children.Add(new TextBlock { Text = "Connect your account, then use it in File Explorer like any other drive.", Margin = new Thickness(0, 5, 0, 18), Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
        var actions = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        letters.Margin = new Thickness(0, 0, 10, 8); letters.Height = 38;
        actions.Children.Add(letters);
        foreach (var b in new[] { login, mount, unmount, open }) { b.Margin = new Thickness(0, 0, 8, 8); b.Padding = new Thickness(12, 8, 12, 8); b.MinHeight = 38; actions.Children.Add(b); }
        driveCard.Children.Add(actions);
        status.Margin = new Thickness(0, 8, 0, 0); status.MinHeight = 24;
        driveCard.Children.Add(new Border { Padding = new Thickness(12, 10, 12, 10), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 6, 0, 0), Child = status });
        panel.Children.Add(Card(driveCard));

        var cacheCard = new StackPanel { Margin = new Thickness(20) };
        cacheCard.Children.Add(new TextBlock { Text = "Local cache", FontSize = 19, FontWeight = FontWeights.SemiBold });
        cacheCard.Children.Add(new TextBlock { Text = "Files are fetched when opened. Changes are staged here, then uploaded to TeraBox after an app closes the file.", Margin = new Thickness(0, 5, 0, 16), Foreground = Brushes.SlateGray, TextWrapping = TextWrapping.Wrap });
        var cacheRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10), LastChildFill = true };
        DockPanel.SetDock(chooseCache, Dock.Right); chooseCache.Padding = new Thickness(10, 7, 10, 7); cacheRow.Children.Add(chooseCache);
        cacheLocation.Margin = new Thickness(0, 0, 12, 0); cacheLocation.ToolTip = CacheSettings.Load(); cacheLocation.Text = "Folder: " + CacheSettings.Load();
        cacheRow.Children.Add(cacheLocation); cacheCard.Children.Add(cacheRow);
        var budgetRow = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        budgetRow.Children.Add(new TextBlock { Text = "Cleanup target", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
        cacheSize.Height = 34; cacheSize.Padding = new Thickness(6); cacheSize.Margin = new Thickness(0, 0, 8, 0);
        budgetRow.Children.Add(cacheSize);
        budgetRow.Children.Add(new TextBlock { Text = "GB", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) });
        foreach (var b in new[] { saveCacheSize, openCache }) { b.Padding = new Thickness(10, 7, 10, 7); b.Margin = new Thickness(0, 0, 8, 0); budgetRow.Children.Add(b); }
        cacheCard.Children.Add(budgetRow);
        transfers.Margin = new Thickness(0, 12, 0, 0); cacheCard.Children.Add(transfers);
        cacheCard.Children.Add(new TextBlock { Text = "The cleanup target is a limit for cached data, not reserved disk space. Active files and pending uploads can temporarily exceed it. Changes apply after reconnecting.", FontSize = 12, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 14, 0, 0), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(Card(cacheCard));

        panel.Children.Add(new TextBlock { Text = "LocalDrive runs in the notification area when this window is closed. Use Exit from its tray menu to safely finish uploads and disconnect.", Foreground = Brushes.SlateGray, FontSize = 12, Margin = new Thickness(2, 16, 2, 0), TextWrapping = TextWrapping.Wrap });
        RefreshLetters();
        login.Click += async (_, _) => await SignInAsync();
        mount.Click += async (_, _) => await ConnectAsync();
        unmount.Click += async (_, _) => await DisconnectAsync();
        chooseCache.Click += (_, _) => ChooseCacheFolder();
        saveCacheSize.Click += (_, _) => SaveCacheBudget();
        openCache.Click += (_, _) => OpenCacheFolder();
        themeToggle.Click += (_, _) => ToggleTheme();
        open.Click += (_, _) => { if (drive != null) Process.Start(new ProcessStartInfo("explorer.exe", drive + "\\") { UseShellExecute = true }); };
        Closing += OnClosing;
        transferTimer.Tick += async (_, _) => await UpdateTransfersAsync();
        ConfigureTray();
        darkTheme = CacheSettings.LoadDarkTheme(); ApplyTheme();
        bool savedLogin = store.Load() != null;
        if (connectOnStart || savedLogin) Loaded += async (_, _) => await ConnectAsync();
        if (savedLogin) status.Text = "Saved TeraBox login found. Connecting automatically…";
    }

    private static Border Card(UIElement content) => new()
    {
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(226, 231, 238)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(14),
        Margin = new Thickness(0, 0, 0, 16),
        Child = content
    };

    private void RefreshLetters()
    {
        letters.Items.Clear();
        var used = DriveInfo.GetDrives().Select(d => d.Name[0]).ToHashSet();
        foreach (char c in Enumerable.Range('D', 23).Select(i => (char)i).Where(c => !used.Contains(c))) letters.Items.Add(c + ":");
        letters.SelectedItem = letters.Items.Contains("T:") ? "T:" : letters.Items.Cast<string>().FirstOrDefault();
    }

    private void ChooseCacheFolder()
    {
        var current = CacheSettings.Load();
        string initial = Directory.Exists(current) ? current : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dialog = new OpenFolderDialog { Title = "Choose where LocalDrive stores cached TeraBox data", InitialDirectory = initial };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            string folder = Path.GetFullPath(dialog.FolderName);
            if (DriveInfo.GetDrives().Any(d => string.Equals(d.RootDirectory.FullName, Path.GetPathRoot(folder), StringComparison.OrdinalIgnoreCase)) == false && !Directory.Exists(folder))
                throw new IOException("That drive is not available.");
            CacheSettings.SaveDirectory(folder);
            cacheLocation.Text = "Cache: " + folder;
            cacheLocation.ToolTip = folder;
            status.Text = process == null
                ? "Cache folder saved. It will be used the next time you connect."
                : "Cache folder saved for the next connection. This drive keeps using its current cache until you disconnect and reconnect.";
        }
        catch (Exception e) { status.Text = "Could not save that cache folder (" + e.GetType().Name + "). Choose an available folder."; }
    }

    private void SaveCacheBudget()
    {
        if (!int.TryParse(cacheSize.Text, out int size) || size is < 1 or > 4096)
        { status.Text = "Enter a cache size from 1 to 4096 GB."; return; }
        try
        {
            CacheSettings.SaveSizeGb(size);
            status.Text = process == null
                ? $"Cache target set to {size} GB for the next connection."
                : $"Cache target set to {size} GB for the next connection. Current mount keeps its existing limit.";
        }
        catch (Exception e) { status.Text = "Could not save the cache size (" + e.GetType().Name + ")."; }
    }

    private void OpenCacheFolder()
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", CacheSettings.Load()) { UseShellExecute = true }); }
        catch (Exception e) { status.Text = "Could not open cache folder (" + e.GetType().Name + ")."; }
    }

    private void ConfigureTray()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open LocalDrive", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Connect drive", null, async (_, _) => { if (process == null) await ConnectAsync(); else RestoreFromTray(); });
        menu.Items.Add("Disconnect drive", null, async (_, _) => { RestoreFromTray(); if (process != null) await DisconnectAsync(); });
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit LocalDrive", null, async (_, _) => await ExitFromTrayAsync());
        trayIcon.ContextMenuStrip = menu;
        trayIcon.MouseDoubleClick += (_, _) => RestoreFromTray();
        trayIcon.Visible = true;
    }

    private void RestoreFromTray()
    {
        Show(); ShowInTaskbar = true; WindowState = WindowState.Normal; Activate();
        if (process != null && !process.HasExited && rc != null)
        {
            transferTimer.Start();
            _ = UpdateTransfersAsync();
        }
    }

    private async Task ExitFromTrayAsync()
    {
        if (busy) return;
        State(true);
        try
        {
            await CleanupAsync();
            closing = true; trayIcon.Visible = false; trayIcon.Dispose(); Close();
        }
        catch (Exception e)
        {
            status.Text = "Disconnect is waiting for uploads or open files (" + e.GetType().Name + "). Keep LocalDrive running and retry.";
            RestoreFromTray(); State(false);
        }
    }

    private void ToggleTheme()
    {
        darkTheme = !darkTheme;
        CacheSettings.SaveTheme(darkTheme);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var background = new SolidColorBrush(darkTheme ? Color.FromRgb(18, 20, 24) : Color.FromRgb(246, 248, 251));
        var card = new SolidColorBrush(darkTheme ? Color.FromRgb(31, 34, 41) : Colors.White);
        var foreground = new SolidColorBrush(darkTheme ? Color.FromRgb(238, 240, 244) : Color.FromRgb(28, 34, 44));
        var buttonBackground = new SolidColorBrush(darkTheme ? Color.FromRgb(48, 52, 62) : Color.FromRgb(235, 239, 245));
        Background = background;
        foreach (var element in Descendants(this))
        {
            if (element is Border border) { border.Background = card; border.BorderBrush = buttonBackground; }
            if (element is TextBlock text) text.Foreground = foreground;
            if (element is Button button) { button.Background = buttonBackground; button.Foreground = foreground; }
            if (element is ComboBox combo) { combo.Background = buttonBackground; combo.Foreground = foreground; }
            if (element is TextBox box) { box.Background = buttonBackground; box.Foreground = foreground; box.CaretBrush = foreground; }
        }
        themeToggle.Content = darkTheme ? "Light mode" : "Dark mode";
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private void State(bool operation)
    {
        busy = operation; login.IsEnabled = !busy && process == null; mount.IsEnabled = !busy && process == null;
        chooseCache.IsEnabled = !busy;
        saveCacheSize.IsEnabled = !busy;
        letters.IsEnabled = !busy && process == null; unmount.IsEnabled = !busy && process != null; open.IsEnabled = !busy && process != null;
    }

    private async Task SignInAsync()
    {
        State(true);
        bool signedIn = false;
        try
        {
            var window = new TeraBoxSignInWindow { Owner = this };
            TeraBoxSession? session = window.ShowDialog() == true ? window.Session : null;
            if (session != null)
            {
                store.Save(session);
                signedIn = true;
                status.Text = "Signed in. Checking your account…";
            }
        }
        catch (Exception e) { status.Text = "Sign-in could not finish (" + e.GetType().Name + "). Retry Sign in to TeraBox."; }
        finally { State(false); }
        if (signedIn) await ConnectAsync();
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port;
    }

    private async Task ConnectAsync()
    {
        State(true);
        string step = "checking-local-engine";
        try
        {
            string rclone = Path.Combine(Program.Engine, "tools", "rclone", "rclone.exe");
            if (!File.Exists(rclone)) throw new FileNotFoundException("Bundled TeraBox engine missing");
            if (letters.SelectedItem is not string selected) { status.Text = "No free drive letter is available."; return; }
            var session = store.Load();
            if (session == null) { status.Text = "Sign in to TeraBox first. The app will check the account and connect automatically after sign-in."; return; }
            if (DriveInfo.GetDrives().Any(d => d.Name.StartsWith(selected, StringComparison.OrdinalIgnoreCase))) { RefreshLetters(); status.Text = "That drive letter is now in use. Choose another."; return; }
            status.Text = "Checking your sign-in and whole-account folder access…";
            ConnectionDiagnostics.Record("checking-terabox-session");
            step = "listing-terabox-root";
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
                await TeraBoxSessionVerifier.VerifyAsync(session, timeout.Token);
            ConnectionDiagnostics.Record("terabox-session-verified");
            status.Text = "TeraBox sign-in verified. Preparing your drive…";

            step = "preparing-local-cache";
            status.Text = "Checking your selected cache folder…";
            string cacheDirectory = CacheSettings.ValidateAndPrepare(CacheSettings.Load(), selected[0] + ":");
            int rcPort = FreePort();
            string rcPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            TeraBoxSessionStore.WriteRcloneConfig(session, mountConfigPath);
            rc = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{rcPort}/"), Timeout = TimeSpan.FromSeconds(5) };
            rc.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("localdrive:" + rcPassword)));
            var info = new ProcessStartInfo(rclone) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            info.ArgumentList.Add("mount"); info.ArgumentList.Add("localdrive:"); info.ArgumentList.Add(selected);
            int cacheSizeGb = CacheSettings.LoadSizeGb();
            foreach (string arg in MountConfiguration.CommonArguments(cacheDirectory, cacheSizeGb)) info.ArgumentList.Add(arg);
            foreach (string arg in new[] { "--stats", "0", "--vfs-write-back", "2s", "--vfs-cache-max-age", "24h", "--buffer-size", "0", "--dir-cache-time", "2m", "--volname", "TeraBox · LocalDrive", "--rc", "--rc-addr", $"127.0.0.1:{rcPort}", "--config", mountConfigPath }) info.ArgumentList.Add(arg);
            info.Environment["RCLONE_RC_USER"] = "localdrive"; info.Environment["RCLONE_RC_PASS"] = rcPassword;
            step = "mounting-windows-drive";
            ConnectionDiagnostics.Record(step);
            process = Process.Start(info) ?? throw new IOException("Could not start filesystem engine");
            // Drain output without persisting potentially sensitive provider URLs or request details.
            process.OutputDataReceived += (_, _) => { }; process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            MountDiagnostics.RecordMount(selected, process.Id, cacheDirectory, cacheSizeGb);
            status.Text = "Mounting drive…";
            ConnectionDiagnostics.Record("mounting-windows-drive");
            bool ready = false;
            for (int i = 0; i < 40; i++)
            {
                if (process.HasExited) throw new IOException("Filesystem engine exited");
                if (Directory.Exists(selected + "\\"))
                {
                    try { using var response = await rc.PostAsJsonAsync("core/version", new { }); ready = response.IsSuccessStatusCode; } catch (HttpRequestException) { }
                    if (ready) break;
                }
                await Task.Delay(500);
            }
            if (!ready) throw new TimeoutException("Mount was not ready");
            TeraBoxSessionStore.DeleteRcloneConfig(mountConfigPath);
            drive = selected;
            transferTimer.Start();
            await UpdateTransfersAsync();
            ConnectionDiagnostics.Record("connected-read-write");
            trayIcon.Text = "LocalDrive — TeraBox connected";
            status.Text = "Connected at " + drive + " — read and write. Changes upload to TeraBox after files close.";
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => Dispatcher.BeginInvoke(async () => { if (!closing && process?.HasExited == true) { await CleanupAsync(); status.Text = "The filesystem engine stopped. Reconnect to retry."; State(false); } });
        }
        catch (Exception e)
        {
            string errorKind = e switch
            {
                RcloneCommandException command => command.AuthenticationFailure ? "auth-rejected" : "rclone-exit-" + command.ExitCode,
                _ => e.GetType().Name
            };
            ConnectionDiagnostics.Record("connection-failed", errorKind, step);
            try { await CleanupAsync(); }
            catch { status.Text = "The engine is still running. Close open drive files, then use Disconnect."; return; }
            status.Text = ConnectionError(e) + " Step: " + step + ".";
            trayIcon.Text = "LocalDrive — disconnected";
        }
        finally { State(false); }
    }

    private static string ConnectionError(Exception error) => error switch
    {
        RcloneCommandException e when e.AuthenticationFailure => "TeraBox rejected the saved sign-in. Sign in again and retry.",
        RcloneCommandException e => "The TeraBox engine returned error " + e.ExitCode + ". The drive was not mounted.",
        FileNotFoundException => "TeraBox sign-in is saved, but the drive engine is missing. Run Setup-LocalDrive.cmd from the extracted setup folder to finish installation, then open LocalDrive from the Start menu.",
        IOException e when e.Message == "Filesystem engine exited" => "TeraBox sign-in is valid, but the drive engine exited while mounting. Confirm WinFsp is installed and choose a free drive letter, then reconnect. The connection log records the failed stage without storing cookies.",
        OperationCanceledException => "TeraBox did not finish its account check within 45 seconds. Check your connection, then retry.",
        InvalidOperationException e when e.Message.StartsWith("Choose a cache folder", StringComparison.Ordinal) => e.Message,
        IOException e when e.Message.StartsWith("The selected cache folder", StringComparison.Ordinal) => e.Message,
        HttpRequestException => "Could not reach the local drive-control endpoint. Reconnect and retry.",
        TimeoutException => "The drive did not become ready. Confirm WinFsp is installed and choose an unused drive letter.",
        _ => "Could not connect (" + error.GetType().Name + "). No drive was mounted. Retry Connect drive; the failed step below identifies where to investigate."
    };

    private async Task DisconnectAsync()
    {
        State(true); status.Text = "Disconnecting…";
        try { await CleanupAsync(); status.Text = "Disconnected. Cached files and your local sign-in are kept."; RefreshLetters(); }
        catch (Exception e) { status.Text = "Disconnect failed (" + e.GetType().Name + "). Close files on the drive and retry."; }
        finally { State(false); }
    }

    private async Task CleanupAsync()
    {
        var active = process;
        if (active != null && !active.HasExited)
        {
            // Preserve cached changes instead of stopping an engine with unfinished uploads.
            if (drive != null)
            {
                if (rc == null) throw new IOException("Cannot verify pending uploads");
                var pending = await ReadTransfersAsync();
                if (!pending.SafeToDisconnect) throw new IOException("Uploads remain pending");
            }
            try { if (rc != null) { using var response = await rc.PostAsJsonAsync("core/quit", new { }); } } catch { }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await active.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { throw new IOException("Close files and try disconnecting again"); }
        }
        process = null; active?.Dispose(); rc?.Dispose(); rc = null;
        transferTimer.Stop(); transfers.Text = "";
        TeraBoxSessionStore.DeleteRcloneConfig(mountConfigPath);
        drive = null;
    }

    private async Task<TransferState> ReadTransfersAsync()
    {
        using var response = await rc!.PostAsJsonAsync("vfs/stats", new { });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return TransferState.Parse(json.RootElement);
    }

    private async Task UpdateTransfersAsync()
    {
        if (checkingTransfers || rc == null || process == null || process.HasExited) return;
        checkingTransfers = true;
        try
        {
            var state = await ReadTransfersAsync();
            transfers.Text = state.Description;
            transferTimer.Interval = state.OutOfSpace || state.Errors > 0 ? TimeSpan.FromSeconds(30)
                : state.Queued > 0 || state.Uploading > 0 ? TimeSpan.FromSeconds(5)
                : TimeSpan.FromSeconds(60);
        }
        catch { transfers.Text = "Upload status unavailable. Keep the app open until pending changes can be checked."; }
        finally { checkingTransfers = false; }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closing) return;
        e.Cancel = true;
        if (busy) { status.Text = "Wait for the current operation to finish before closing."; return; }
        Hide(); ShowInTaskbar = false;
        transferTimer.Stop();
        trayIcon.Visible = true;
        trayIcon.Text = process == null ? "LocalDrive — disconnected" : "LocalDrive — TeraBox connected";
        status.Text = "LocalDrive is running in the notification area. Use its menu to reopen or exit.";
    }
}


