# LocalDrive checkpoint

Updated 25 October 2026. Source commit: `02092a8` (`Shrink setup and reduce idle polling`). Remote repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Completed

- Changed the distributable to a compact online setup bundle using framework-dependent single-file publishing. Large runtime/engine payloads are fetched only when setup needs them.
- Setup verifies rclone-extra by its pinned SHA-256, verifies WinFsp by checksum and signature, and checks Microsoft signatures for WebView2 and .NET Desktop Runtime installers.
- Reduced UI status polling to once per minute when visibly idle, five seconds during active uploads, and stopped UI polling while the window is hidden in the tray.
- Increased directory listing cache lifetime to two minutes to reduce repeated cloud lookups.
- Updated the README and feature/third-party documentation to describe online setup size, dependency downloads, and idle behavior.

## Verification

- `D:\clone\.tools\dotnet\dotnet.exe build LocalDrive.csproj -c Release`: succeeded with 0 warnings and 0 errors.
- `Build-Standalone.ps1`: succeeded and produced `LocalDrive-1.3.1-Setup-win-x64.zip` (499,716 bytes; SHA-256 `14AB58FB517BB6DF8A1E6BA7161835488AA4756A784BF30BC2D97D0245030F4F`).
- Package inventory contains the app, WebView2 loader, setup scripts, README, and notices; it excludes rclone, WinFsp, WebView2, and .NET runtime installers.
- PowerShell syntax parsing for build, install, and uninstall scripts: passed. `git diff --check`: passed.
- No live installation, TeraBox sign-in/mount, or measured runtime CPU sampling was performed. The UI polling is stopped while hidden; exact whole-process 0% CPU is not guaranteed.

## Exact next steps

1. Commit this checkpoint update, push `main`, and verify the remote head.
2. Push tag `v1.3.1` to build and publish the compact setup ZIP; confirm the release asset and workflow result.
