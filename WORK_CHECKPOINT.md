# LocalDrive checkpoint

Updated 25 October 2026. Sign-in fix source commit: `2dffdef` (`Improve TeraBox sign-in cookie capture`). Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Completed

- Sign-in now asks WebView2 for the active TeraBox origin first, then supported primary/legacy TeraBox origins; it selects a cookie set only when it contains the required session cookie and deduplicates repeated cookie names.
- Added support for the bare `1024terabox.com` host as well as its subdomains.
- If no session cookie is available, the dialog tells the user to finish sign-in and return to the TeraBox files page. It records only a generic diagnostic stage.
- The connection status now explicitly separates a verified sign-in from drive setup/mount. A missing engine message directs users to complete setup; an early mount exit says the sign-in is valid and points to WinFsp/drive-letter checks.
- README and feature status document the broader sign-in host handling.

## Verification

- `D:\clone\.tools\dotnet\dotnet.exe build LocalDrive.csproj -c Release`: succeeded with 0 warnings and 0 errors.
- `Build-Standalone.ps1`: succeeded; `LocalDrive-1.3.2-Setup-win-x64.zip` is 500,729 bytes (~489 KiB), SHA-256 `6BC733C12CE088A2CE82569EAA0596365CB391110E44BC877D518809E6AA6F04`.
- PowerShell syntax parsing and `git diff --check` passed. Package inventory excludes the large engine/runtime payloads.
- The current saved session was verified against TeraBox using `--diagnose-connection` (exit code 0). T: was not mounted and no rclone mount process was running at that time.
- I have not tested login using a fresh Google/QR session against the alternate-domain capture path. No secrets or cookie values were printed or logged.

## Exact next steps

1. Commit this checkpoint update and push `main`.
2. Push tag `v1.3.2`, then verify the GitHub build and release asset.
3. Install 1.3.2 using `Setup-LocalDrive.cmd` and test a fresh sign-in/mount on this PC.
