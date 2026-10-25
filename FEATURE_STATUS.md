# LocalDrive feature status

LocalDrive is a standalone Windows desktop app. It uses an open-source TeraBox backend in the pinned `rclone-extra` release, started as a child process. This backend is community-maintained, nonofficial, and upstream marks the TeraBox integration beta. The app captures browser cookies in its own WebView2 profile and verifies the account root through the backend before saving the session.

| Capability | Status |
| --- | --- |
| TeraBox sign-in | Embedded WebView2; reads the active TeraBox origin plus supported account domains, requires the session cookie, verifies a root listing, and stores the session with Windows DPAPI. Personal Chrome cookie import is intentionally absent. |
| Whole-account root | Uses the cookie-authenticated TeraBox backend; not limited to an official API app folder. Provider behavior can change. |
| Virtual drive | rclone + WinFsp, configurable letter, read/write mount, VFS full cache, executable permissions 0755. |
| Cache | User-selected writable path and 1–4096 GB cleanup target; changes apply after reconnect. |
| Background operation | Notification-area tray, close-to-tray, saved-session reconnect, controlled disconnect that checks VFS pending uploads. UI transfer polling stops while the window is hidden; while visible it polls once a minute when idle and more often during uploads. |
| Appearance | Light and dark themes. |
| Git-ready bundle | One downloadable self-extracting setup EXE embeds the app and compact TeraBox engine. Setup fetches signed .NET, WebView2, and WinFsp prerequisites only when missing. Engine checksum and installer signatures are verified before install. GitHub Actions publishes the EXE on `v*` tags. The unsigned EXE may trigger a Windows SmartScreen warning. |
| Limitations | TeraBox cookies expire and the backend is unofficial beta. Google login may refuse an embedded browser; use TeraBox QR/email when offered. |

The compact backend and one-file installer have been built locally. A clean installation, live TeraBox sign-in, and read/write transfer still require verification in a Windows user session with the account available.
