# Feature status

Current source version: **1.4.1**.

| Area | Current behavior | Known limit |
| --- | --- | --- |
| TeraBox sign-in | Embedded WebView2; session encrypted with Windows DPAPI for the current user. | The backend is unofficial beta. A browser login can succeed while a later account or mount request is rejected. |
| Account scope | Mounts the account through the community TeraBox backend. | Provider behavior, throttling, and cookie expiry can interrupt access. |
| Windows drive | rclone `cmount` with WinFsp; configurable drive letter, read/write, 0755 permissions, full VFS cache. | Requires Windows x64 and WinFsp. Executable launch behavior is also subject to Windows and application-specific restrictions. |
| Cache | User-selectable writable folder and a 1–4096 GB cleanup target. | The target is not reserved disk space. Open files and pending uploads may temporarily exceed it. Changes apply after reconnect. |
| Background | Notification-area operation; transfer polling stops while the window is hidden. Starting a second instance restores the existing window. Disconnect/exit checks pending uploads. | The engine remains resident while mounted. |
| Appearance | Light and dark themes reapply after WPF creates its controls; colors cover cards, text, controls, and disabled states. Both themes were inspected in the Windows UI preview. | Native Windows control rendering can still vary with system accessibility settings. |
| Distribution | Single setup EXE bundles LocalDrive and the compact TeraBox engine. | WinFsp, .NET Desktop Runtime, and WebView2 download during setup only if absent. WinFsp SHA-256 and Authenticode are checked before install. Setup is unsigned and may show an unknown-publisher warning. |

## Verification boundary

Local source/build and isolated drive-engine tests do not prove that the current TeraBox session can mount or transfer files. A live TeraBox read/write test requires a valid session accepted by the provider; release workflow results are published with each GitHub release.
