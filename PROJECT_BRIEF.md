# LocalDrive project brief

LocalDrive is a stand-alone Windows x64 app for mounting a user's TeraBox account as a read/write drive. Its product goals are:

- Provide a clear WPF interface for sign-in, drive connection, transfer status, cache settings, and light/dark appearance.
- Store the TeraBox web session with Windows DPAPI and use the user's own embedded WebView2 login profile.
- Mount with the pinned, compact TeraBox-only rclone engine through WinFsp; keep account access and cloud file operations in the engine.
- Allow a user-selected cache folder and cleanup target, explain staged writes, and protect pending uploads during disconnect/exit.
- Stay in the notification area when the main window closes and reduce UI polling while hidden or idle.
- Ship as one setup EXE, bundle the signed WinFsp installer and compact engine, fetch signed Microsoft runtimes only when missing, and publish via GitHub Releases.
- Keep the repository source-focused: exclude generated packages, runtime downloads, local logs, settings, cache data, and authentication material.

The TeraBox backend is unofficial beta. A successful sign-in capture does not guarantee that the provider will accept the session when the drive mount starts. Record build, offline driver, cached-session, live connection, and file-operation results separately; never describe a local test as proof that remote TeraBox access works.
