# Feature status

Current source version: **1.3.9**.

| Area | Current behavior | Known limit |
| --- | --- | --- |
| TeraBox sign-in | Embedded WebView2; session encrypted with Windows DPAPI for the current user. | The backend is unofficial beta. A browser login can succeed while a later account or mount request is rejected. |
| Account scope | Mounts the account through the community TeraBox backend. | Provider behavior, throttling, and cookie expiry can interrupt access. |
| Windows drive | rclone `cmount` with WinFsp; configurable drive letter, read/write, 0755 permissions, full VFS cache. | Requires Windows x64 and WinFsp. Executable launch behavior is also subject to Windows and application-specific restrictions. |
| Cache | User-selectable writable folder and a 1–4096 GB cleanup target. | The target is not reserved disk space. Open files and pending uploads may temporarily exceed it. Changes apply after reconnect. |
| Background | Notification-area operation; transfer polling stops while the window is hidden. Disconnect/exit checks pending uploads. | The engine remains resident while mounted. |
| Appearance | Light and dark themes with explicit control, hover, selection, disabled-state, and status-text colors. | The current session has an active hidden mount process and no LocalDrive window to inspect; fresh light/dark screenshots remain unverified. |
| Distribution | Single setup EXE bundles LocalDrive, compact TeraBox engine, and signed WinFsp installer. | .NET Desktop Runtime and WebView2 download during setup if absent. Setup is unsigned and may show an unknown-publisher warning. |

## Verification boundary

Local source/build and isolated drive-engine tests do not prove that the current TeraBox session can mount or transfer files. A live TeraBox read/write test requires a valid session accepted by the provider. See `WORK_CHECKPOINT.md` for the latest run-specific results.
