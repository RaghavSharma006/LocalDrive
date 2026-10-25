using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Button = System.Windows.Controls.Button;

namespace LocalDrive;

internal sealed class TeraBoxSignInWindow : Window
{
    internal TeraBoxSession? Session { get; private set; }
    private readonly WebView2 browser = new();
    private readonly TextBlock status = new() { Text = "Sign in on TeraBox below. LocalDrive will verify the account before saving it.", Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap };
    private readonly Button connect = new() { Content = "Connect my TeraBox", Margin = new Thickness(16), Padding = new Thickness(16, 10, 16, 10), IsEnabled = false };
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private static readonly string[] CookieOrigins =
    [
        "https://www.terabox.com/",
        "https://terabox.com/",
        "https://www.1024terabox.com/",
        "https://1024terabox.com/"
    ];
    private bool checking, closed;
    private string lastCandidate = "";
    private DateTimeOffset lastAttempt;

    internal TeraBoxSignInWindow()
    {
        Title = "Sign in to TeraBox — LocalDrive";
        Width = 1040; Height = 800; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel(); Content = layout;
        DockPanel.SetDock(status, Dock.Top); layout.Children.Add(status);
        DockPanel.SetDock(connect, Dock.Bottom); layout.Children.Add(connect); layout.Children.Add(browser);

        Loaded += async (_, _) =>
        {
            try
            {
                ConnectionDiagnostics.Record("opening-terabox-sign-in");
                var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Program.Home, "browser"));
                await browser.EnsureCoreWebView2Async(environment);
                browser.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                browser.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                browser.CoreWebView2.NavigationCompleted += async (_, e) =>
                {
                    if (!e.IsSuccess) { status.Text = "TeraBox could not load (" + e.WebErrorStatus + "). Check the network and retry."; return; }
                    await CaptureAsync(explicitClick: false);
                };
                browser.CoreWebView2.NewWindowRequested += async (_, e) =>
                {
                    using var deferral = e.GetDeferral();
                    var popupBrowser = new WebView2();
                    var popup = new Window { Title = "TeraBox sign-in", Owner = this, Width = 900, Height = 760, Content = popupBrowser, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    popup.Closed += (_, _) => popupBrowser.Dispose();
                    try
                    {
                        await popupBrowser.EnsureCoreWebView2Async(environment);
                        e.NewWindow = popupBrowser.CoreWebView2; e.Handled = true; popup.Show();
                    }
                    catch { popupBrowser.Dispose(); e.Handled = true; status.Text = "The sign-in popup could not open. Retry on the TeraBox page."; }
                };
                browser.CoreWebView2.Navigate("https://www.terabox.com/login");
                connect.IsEnabled = true;
                timer.Start();
            }
            catch (Exception e) { status.Text = "TeraBox sign-in could not start (" + e.GetType().Name + "). Check that Microsoft WebView2 Runtime is installed."; }
        };
        timer.Tick += async (_, _) => await CaptureAsync(explicitClick: false);
        connect.Click += async (_, _) => await CaptureAsync(explicitClick: true);
        Closed += (_, _) => { closed = true; timer.Stop(); browser.Dispose(); };
    }

    private async Task CaptureAsync(bool explicitClick)
    {
        if (checking || closed || browser.CoreWebView2 == null) return;
        checking = true;
        try
        {
            if (!Uri.TryCreate(browser.CoreWebView2.Source, UriKind.Absolute, out var uri) || !IsTeraBox(uri))
            { if (explicitClick) status.Text = "Return to the TeraBox page after signing in, then connect again."; return; }
            if (!explicitClick && !uri.AbsolutePath.StartsWith("/main", StringComparison.OrdinalIgnoreCase) && !uri.AbsolutePath.StartsWith("/disk", StringComparison.OrdinalIgnoreCase)) return;

            string? cookieHeader = await ReadSessionCookieHeaderAsync(uri);
            if (string.IsNullOrWhiteSpace(cookieHeader))
            {
                ConnectionDiagnostics.Record("terabox-cookie-not-found", "session-cookie-unavailable");
                if (explicitClick) status.Text = "TeraBox opened, but LocalDrive could not find the account session. Finish sign-in, return to your TeraBox files page, wait a moment, then try again.";
                return;
            }

            TeraBoxSessionStore.ValidateCookie(cookieHeader);
            string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookieHeader)));
            if (!explicitClick && fingerprint == lastCandidate && DateTimeOffset.UtcNow - lastAttempt < TimeSpan.FromSeconds(30)) return;
            lastCandidate = fingerprint; lastAttempt = DateTimeOffset.UtcNow;

            status.Text = "Checking that this TeraBox sign-in can access your account…";
            ConnectionDiagnostics.Record("verifying-terabox-session");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            await TeraBoxSessionVerifier.VerifyAsync(new TeraBoxSession(cookieHeader, DateTimeOffset.UtcNow), timeout.Token);
            if (closed || !IsVisible) return;
            Session = new TeraBoxSession(cookieHeader, DateTimeOffset.UtcNow);
            ConnectionDiagnostics.Record("terabox-sign-in-received");
            timer.Stop();
            DialogResult = true;
        }
        catch (RcloneCommandException e)
        {
            if (!closed) status.Text = e.AuthenticationFailure
                ? "TeraBox did not accept this sign-in yet. Finish signing in or use the QR/email option, then retry."
                : "TeraBox connection check failed (engine code " + e.ExitCode + "). Wait a moment and retry.";
            ConnectionDiagnostics.Record("terabox-login-check-failed", e.AuthenticationFailure ? "auth-rejected" : "rclone-exit-" + e.ExitCode);
        }
        catch (OperationCanceledException) { if (!closed) status.Text = "TeraBox took too long to answer. Check your connection and retry."; }
        catch (Exception e) { if (!closed && explicitClick) status.Text = "Could not complete sign-in (" + e.GetType().Name + "). Wait for the page to finish and retry."; }
        finally { checking = false; }
    }

    private async Task<string?> ReadSessionCookieHeaderAsync(Uri currentUri)
    {
        var origins = new[] { currentUri.GetLeftPart(UriPartial.Authority) + "/" }
            .Concat(CookieOrigins)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (string origin in origins)
        {
            try
            {
                var cookies = await browser.CoreWebView2.CookieManager.GetCookiesAsync(origin);
                var unique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var cookie in cookies)
                {
                    if (!string.IsNullOrWhiteSpace(cookie.Name) && !string.IsNullOrWhiteSpace(cookie.Value))
                        unique.TryAdd(cookie.Name, cookie.Value);
                }
                if (unique.TryGetValue("ndus", out _) && unique.Count > 0)
                    return string.Join("; ", unique.Select(cookie => cookie.Key + "=" + cookie.Value));
            }
            catch (Exception) { /* Try the next supported TeraBox web origin. */ }
        }
        return null;
    }

    private static bool IsTeraBox(Uri uri) => uri.Scheme == "https" &&
        (uri.Host.Equals("terabox.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".terabox.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.Equals("1024terabox.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".1024terabox.com", StringComparison.OrdinalIgnoreCase));
}
