# LocalDrive feature status

LocalDrive is a standalone Windows desktop app. It uses an open-source TeraBox backend in the pinned `rclone-extra` release, started as a child process. This backend is community-maintained, nonofficial, and upstream marks the TeraBox integration beta. The app captures browser cookies in its own WebView2 profile and verifies the account root through the backend before saving the session.

| Capability | Status |
| --- | --- |
| TeraBox sign-in | Embedded WebView2; captures the TeraBox cookie header, verifies a root listing, and stores the session with Windows DPAPI. Personal Chrome cookie import is intentionally absent. |
| Whole-account root | Uses the cookie-authenticated TeraBox backend; not limited to an official API app folder. Provider behavior can change. |
| Virtual drive | rclone + WinFsp, configurable letter, read/write mount, VFS full cache, executable permissions 0755. |
| Cache | User-selected writable path and 1–4096 GB cleanup target; changes apply after reconnect. |
| Background operation | Notification-area tray, close-to-tray, saved-session reconnect, controlled disconnect that checks VFS pending uploads. |
| Appearance | Light and dark themes. |
| Git-ready bundle | Source-only repository; pinned rclone-extra, WinFsp and WebView2 downloads are fetched and checksum/signature-verified by the package script. GitHub Actions builds a ZIP release on `v*` tags. |
| Limitations | TeraBox cookies expire and the backend is unofficial beta. Google login may refuse an embedded browser; use TeraBox QR/email when offered. |

The new backend path has been compiled but not yet run against the live TeraBox account. The old LocalDrive process currently owns T:; keep that mount intact until the new package is ready and a deliberate reconnect can be performed.
