# LocalDrive checkpoint

Updated 25 October 2026. Source commit: `02092a8` (`Shrink setup and reduce idle polling`). Release tag/source commit: `v1.3.1` / `a4fe785`. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Completed

- Changed distribution to a compact online setup bundle using framework-dependent single-file publishing. The large runtime and engine payloads are fetched only when setup needs them.
- Setup verifies rclone-extra by its pinned SHA-256, verifies WinFsp by checksum and signature, and checks Microsoft signatures for WebView2 and .NET Desktop Runtime installers.
- Reduced UI status polling to once per minute when visibly idle, five seconds during active uploads, and stopped UI polling while the window is hidden in the tray.
- Increased directory listing cache lifetime to two minutes to reduce repeated cloud lookups.
- Updated the README and feature/third-party documentation to describe the online setup size, runtime downloads, and idle behavior.
- Pushed `main` and tag `v1.3.1`.
- GitHub Actions run `40174388737` completed successfully, including build, package, release upload, and workflow artifact steps.
- Published the compact Windows x64 release at https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.1.

## Verification

- `D:\clone\.tools\dotnet\dotnet.exe build LocalDrive.csproj -c Release`: succeeded with 0 warnings and 0 errors.
- `Build-Standalone.ps1`: succeeded; local package `LocalDrive-1.3.1-Setup-win-x64.zip` is 499,716 bytes (~488 KiB), SHA-256 `14AB58FB517BB6DF8A1E6BA7161835488AA4756A784BF30BC2D97D0245030F4F`.
- The GitHub Release contains `LocalDrive-1.3.1-Setup-win-x64.zip` (499,946 bytes).
- Package inventory contains the app, WebView2 loader, setup scripts, README, and notices; it excludes rclone and Windows runtime installers.
- PowerShell syntax parsing for build, install, and uninstall scripts passed. `git diff --check` passed.
- At release verification, local `main` matched `origin/main` at commit `a4fe785`.
- No live installation, TeraBox sign-in/mount, or runtime CPU measurement was performed. UI polling stops while hidden; exact whole-process 0% CPU is not guaranteed.

## Remaining work

- To validate on this PC, install the release, connect TeraBox, exercise file operations, and measure idle CPU with the drive mounted.
