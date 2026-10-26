# LocalDrive

LocalDrive is a Windows desktop app that mounts a TeraBox account as a drive letter. Browse files in File Explorer and compatible Windows apps; choose where its working cache is stored.

> **Status:** Windows x64, version 1.3.9. The latest published installer is available from [GitHub Releases](https://github.com/RaghavSharma006/LocalDrive/releases). The TeraBox connector is an unofficial, community-maintained beta. A successful browser sign-in does not guarantee that TeraBox will accept the session when connecting the drive.

## Install

1. Download `LocalDrive-<version>-Setup-win-x64.exe` from [Releases](https://github.com/RaghavSharma006/LocalDrive/releases).
2. Run setup. Approve the normal Windows administrator prompt if WinFsp must be installed.
3. Sign in to TeraBox in LocalDrive, select a free drive letter, then choose **Connect drive**.

The one-file installer bundles LocalDrive, its compact TeraBox-only rclone engine, and the unmodified signed WinFsp installer. It downloads signed Microsoft .NET Desktop Runtime and WebView2 installers only when those prerequisites are missing. Setup needs internet access for those downloads. Windows SmartScreen can identify the setup EXE as an unknown publisher because the project does not have a code-signing certificate.

## Use the drive and cache

- **Connect:** Sign-in runs in LocalDrive's own WebView2 profile. The app does not import cookies from Chrome or another browser. Select a free drive letter and connect; the engine checks the saved session as part of mounting.
- **Read:** Files are fetched from TeraBox as Windows applications request them. rclone's full VFS cache holds fetched data and supports random access.
- **Write:** Changes are staged in the cache and uploaded after the application closes the file. Wait for pending uploads before disconnecting or exiting.
- **Cache folder:** Choose a writable folder on any available drive other than the selected mount letter. Moving the cache to another disk moves future cache use there; it does not copy or erase data from your TeraBox account.
- **Cleanup target:** The 1–4096 GB value is a cleanup limit for old cached data, not reserved space. Open files and pending uploads may temporarily exceed it. Folder and size changes apply after reconnecting.
- **Background:** Closing the window sends LocalDrive to the notification area. The mounted engine stays running to serve Windows file requests and complete uploads. When hidden, the UI stops transfer polling. Use the tray menu to reconnect, disconnect, reopen the window, or exit.

Directory listings are cached for up to two minutes. Recent remote changes may take that long to appear. The mount uses 0755 file and directory permission modes for Windows execute access; Windows security and individual applications can still prevent an executable from launching from a cloud-backed drive.

## Sign-in and privacy

The login session is stored under `%LOCALAPPDATA%\LocalDrive` encrypted with Windows DPAPI for the current Windows user. A temporary engine configuration is created for mount startup and removed after the mount is ready. Connection diagnostics store a failure stage and sanitized category; they do not store raw provider output, cookies, signed URLs, or remote file names. Settings and the selected cache are local to this Windows profile unless the user chooses another cache location.

This app uses a community-maintained, unofficial TeraBox backend because the official API does not provide unrestricted access to a full personal account for this use. TeraBox can change or restrict its web service, so sign-in or mounting can stop working without a LocalDrive update. Review [third-party notices](THIRD_PARTY_NOTICES.md) for versions, sources, and licenses.

## Troubleshooting

- **Sign-in completes but the drive does not connect:** sign-in captures a browser session; the provider can still reject it when the drive starts. Try signing in again. Check `%LOCALAPPDATA%\LocalDrive\connection-status.json` for the failed stage and category.
- **The engine exits during mount:** check `%LOCALAPPDATA%\LocalDrive\mount-failure.json`. It contains the exit code and a sanitized failure category. It intentionally omits raw engine output and credentials.
- **WinFsp is missing:** run setup again and approve its signed driver installation. Restart Windows if the installer requests it.
- **Drive letter unavailable:** choose another unused letter.
- **Cache folder rejected:** choose a writable directory on an attached drive, separate from the selected virtual drive.
- **Disconnect waits or fails:** close files opened from the virtual drive and wait for uploads to finish before retrying.
- **Executable will not launch:** first verify the file can be read and copied. Execution also depends on Windows' loader, the remote file response, and application requirements; LocalDrive does not disable Windows protections.

## Build and verify

Requirements for a source build: Windows x64 and the .NET 10 SDK. A tagged release additionally builds the compact engine from the pinned `rclone-extra` source and embeds the official WinFsp installer; Git and Go are used by the release workflow.

```powershell
dotnet publish .\LocalDrive.csproj -c Release -r win-x64 --self-contained false
dotnet run --project .\LocalDrive.csproj -c Release -- --verify-file-ops
```

To make the local one-file setup package without a release engine, run:

```powershell
./Build-Standalone.ps1
```

That source package may fetch its pinned engine during installation. GitHub Actions builds the compact engine and packages the one-file installer when a `v*` tag is pushed. See [the release workflow](.github/workflows/release.yml). Generated setup files, downloaded dependencies, build output, logs, and cache data are excluded from Git.

## Project and license

- [Feature status and known limits](FEATURE_STATUS.md)
- [Release history](CHANGELOG.md)
- [Project brief](PROJECT_BRIEF.md)
- [Third-party notices and licenses](THIRD_PARTY_NOTICES.md)
- [Security reporting](SECURITY.md)

No license is currently granted for the LocalDrive application source. The third-party components retain the licenses described in their notices.
