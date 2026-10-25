# LocalDrive

LocalDrive mounts your TeraBox account as a Windows drive letter. Browse and manage cloud files in File Explorer and other Windows apps, with a local cache that you choose.

## Download and install

1. Download `LocalDrive-<version>-Setup-win-x64.zip` from [GitHub Releases](https://github.com/RaghavSharma006/LocalDrive/releases).
2. Extract the ZIP and run `Setup-LocalDrive.cmd`.
3. Accept the Windows prompt if setup needs to install the WinFsp filesystem driver.
4. Open **LocalDrive** from the Start menu and sign in.

The ZIP is a compact online installer. It contains the LocalDrive app and setup scripts; setup downloads the pinned TeraBox engine and installs signed Windows runtimes only when missing. An internet connection is needed during setup. The installed app uses the .NET 10 Desktop Runtime, WebView2 Runtime, WinFsp, and the TeraBox engine. Downloads are checksum-verified or signature-verified before use. The full engine still takes disk space after installation; keeping it out of every user's download ZIP makes the package much smaller.

Settings and the encrypted sign-in are kept under `%LOCALAPPDATA%\LocalDrive`.

## Connect your account

1. Select **Sign in to TeraBox** and finish signing in inside the LocalDrive window.
2. LocalDrive checks that the session can list your account before it saves the sign-in.
3. Choose a free drive letter and select **Connect drive**.
4. Select **Open drive** or browse to the drive in File Explorer.

LocalDrive keeps its own WebView2 sign-in profile. It does not read another browser's cookie database. The session is encrypted for your Windows user with DPAPI. A temporary rclone configuration is access-restricted to your account and removed after mount startup. If the sign-in expires, sign in again.

## How files and cache work

- **Reading:** rclone fetches remote file data as Windows applications read it and stores useful data in the cache.
- **Writing:** rclone stages changes in the cache and uploads them after the application closes the file. The transfer status appears in LocalDrive.
- **Cache location:** choose any writable local folder or drive, except the drive letter currently used for the mount. An SSD can improve repeated reads; another disk can save space on your system drive.
- **Cleanup target:** the GB value limits old cached data that rclone can remove. It does not reserve that amount of disk space. Open files and pending uploads can temporarily exceed it. New settings take effect after disconnecting and reconnecting.
- **Safe disconnect:** LocalDrive checks for pending uploads before stopping the mount. Close files that are open from the virtual drive, wait for uploads, then disconnect.

When the window is open, transfer checks slow to once a minute while idle and speed up while uploads are active. Closing the window stops UI polling and keeps the app in the notification area; the rclone mount continues handling drive requests and queued transfers. Open the window to refresh transfer status. Use the tray menu to reopen, disconnect, or exit. Exit checks for pending uploads before quitting.

Directory listings are cached for up to two minutes to reduce repeated cloud lookups. New remote changes may take that long to appear unless the folder is refreshed after the cache expires.

## Build from source

Requirements: Windows x64 and the .NET 10 SDK. The build script creates the same compact online setup archive as the release workflow. Runtime installers and the engine are fetched and verified by setup, not bundled into the ZIP.

```powershell
./Build-Standalone.ps1
```

The output archive is named `LocalDrive-<version>-Setup-win-x64.zip`. The source repository excludes generated binaries, downloaded runtimes, archives, caches, and logs. Third-party notices and required licenses are included in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) and [`licenses/`](licenses/).

## Security and support limits

The TeraBox connection uses a community-maintained, unofficial backend because a general public API for mounting a complete personal account is not available. The backend uses the TeraBox web session and can stop working if the provider changes its service. Its release is pinned and verified by SHA-256 during setup. Review [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) for the exact source, version, and license.

The app does not bypass Windows security. Whether a particular executable runs from a cloud-backed drive depends on Windows and that program's own requirements. Cloud latency, provider throttling, or expired sign-in can affect response times and transfers.

## Troubleshooting

- **Sign-in completes but drive does not connect:** retry once; then review `%LOCALAPPDATA%\LocalDrive\connection-status.json` for the failed stage. It records generic stage/error details, not cookies, signed URLs, or file names.
- **Drive letter is unavailable:** choose a free letter, then connect again.
- **Cache cannot be used:** choose a writable folder on an available drive and keep it separate from the selected mount letter.
- **Disconnect waits:** close applications that are using files on the drive and wait for pending uploads to finish.
- **Drive does not mount:** confirm WinFsp is installed, then reconnect. Restart Windows if a driver installation requested it.
- **Setup says it cannot download a dependency:** retry on a stable connection. Setup verifies the published engine checksum and Microsoft/WinFsp signatures before installation.

## Repository contents

`PROJECT_BRIEF.md` describes the product and packaging scope. `FEATURE_STATUS.md` lists the current feature status and known limits. `.gitignore` excludes build output, generated archives, logs, and vendored runtime payloads.

The `.github/workflows/release.yml` workflow builds a Windows x64 setup archive on manual dispatch and publishes a GitHub Release when a `v*` tag is pushed.
