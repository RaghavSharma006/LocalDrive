# LocalDrive checkpoint

Updated 25 October 2026. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Current implementation

- Version: `1.3.8`
- Implementation commit: `05b3d33132cb1c4bc8eec9af0b77f94344552d19`.
- Checkpoint commit before release: `5f83dacdb6433ccc5681cf8477fa42bd00a5421a`.
- Release purpose: build and distribute a compact, TeraBox-only rclone engine while keeping the one-click app downloads small.

## Completed

- Added `Build-SlimRclone.ps1`. It clones rclone-extra tag `v1.75.2-extra`, verifies source commit `56bf13885494819bc801302ddf881279bdac7262`, strips unused backend/command registrations and optional plugin registration, then builds Windows x64 with `CGO_ENABLED=0`, `cmount`, `-trimpath`, and stripped debug data.
- Engine retains TeraBox, `mount`/Windows `cmount`, `lsjson`, `version`, and built-in help/completion. Setup includes the rclone MIT license.
- Updated tagged GitHub releases to build and attach `LocalDrive-Rclone-<version>-win-x64.zip`; setup embeds that release URL and the exact archive SHA-256.
- Updated installer to checksum-verify the compact archive, stage/swap its executable with rollback on failure, and replace older unmarked full-size engines. Existing TeraBox session and cache are not altered.
- Bumped the app to `1.3.4`; updated README and third-party notices.

## One-file low-idle package (current work)

- Implementation commit: `9f1baf151834764d1bfc68526831b82bb7b121de`.
- Bumped app to `1.3.5`. Tagged releases now expose a single `LocalDrive-<version>-Setup-win-x64.exe`; it embeds the app and checksum-verified slim TeraBox engine. Signed .NET Desktop Runtime, WebView2, and WinFsp installers download only when missing.
- Removed the release setup's dead engine-archive download URL. If the embedded engine is absent or damaged, the installer now reports that the setup should be downloaded again. Source-only builds retain the upstream fallback.
- Disabled rclone periodic console stats and kept the existing zero read buffer; the app already stops transfer polling while hidden. The mount engine stays resident while T: is mounted.
- Local package: `LocalDrive-1.3.5-Setup-win-x64.exe`, `8,683,520` bytes (8.28 MiB). Its intermediate setup ZIP is `8,487,329` bytes and includes the `7,993,714`-byte compact engine archive.
- Verification: .NET publish and IExpress package build succeeded; installer PowerShell parsed; embedded engine digest matched installer pin; package contents checked for app, installer and exactly one engine archive; `git diff --check` passed. Live installation, account sign-in, mount and remote file operations not run.

## Bundled WinFsp installer (current work)

- Implementation commit: `a3b1fac7e85736d1d18471e6b81ca27cf9c92d21`.
- Version `1.3.6` keeps the existing rclone `mount`/WinFsp architecture and embeds the official unmodified WinFsp 2.1.25156 MSI inside the single setup EXE. If WinFsp is already installed, setup skips it; if absent, setup verifies the MSI SHA-256 and Authenticode signer, then installs it with the normal Windows administrator prompt.
- Release build fetches the official MSI from WinFsp's v2.1 release and the build script verifies its hash and `NAVIMATICS LLC` signature before bundling. The MSI is excluded from the installed app directory. The WinFsp license accompanies the packaged installer.
- Verified MSI: `2,191,360` bytes; SHA-256 `073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A`; Authenticode status `Valid`.
- Local package: `LocalDrive-1.3.6-Setup-win-x64.exe`, `10,084,352` bytes (~9.62 MiB). The inner setup ZIP is `9,892,154` bytes and includes exactly one WinFsp MSI, the compact rclone engine, app, installer and license.
- Verification: .NET publish and IExpress packaging succeeded; PowerShell scripts parsed; package entries and MSI digest/signature checked; compact engine smoke checks passed for TeraBox, `mount`, `lsjson`, and version, with `serve webdav` absent; `git diff --check` passed. Installation, live account sign-in, and TeraBox transfers have not been run.

## Verification

- Shipped engine before: `84,690,432` bytes. Compact executable: `20,217,344` bytes (~76.1% smaller).
- Local test engine ZIP: `7,993,714` bytes. Final release engine ZIP: `7,993,714` bytes; SHA-256 `85041ADE82445E753B23233757EB13038B37C2D25A85E48686EFDDBD99E08B49`.
- Final release assets: setup EXE `598,016` bytes; setup ZIP `501,665` bytes; engine ZIP `7,993,714` bytes.
- `Build-SlimRclone.ps1` succeeded locally and in CI. Its command smoke checks passed for TeraBox, mount, lsjson, and version. Extracted engine archive contained only `rclone.exe` and `COPYING`.
- `Build-Standalone.ps1` completed .NET publish and package creation. All PowerShell scripts parsed; generated installer URL and archive digest matched; `git diff --check` passed.
- REA/Ghidra was activated after the Codex restart. REA opened the original and compact native files as x64 Windows PE. Safe artifact inventory of the exact final compact engine verified one PE payload of `20,217,344` bytes with SHA-256 `052DDDC60BF85B20D836A0631914509B72C3A1EDDC4342D733117F36DC6E9797`. Ghidra's full function/string overviews timed out at 300 seconds for both Go binaries; no complete decompilation is claimed.
- GitHub Actions run `40179404245` succeeded. GitHub release metadata confirms all three assets are attached and live at https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.4.
- No live TeraBox session, mount/unmount, remote read/write/delete, or executable-from-T: test was run. Those require a Windows user session and authenticated account.

## Deployment

- Published release: https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.4
- GitHub Actions: https://github.com/RaghavSharma006/LocalDrive/actions/runs/40179404245 (success).
- Published release: https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.5
- GitHub Actions: https://github.com/RaghavSharma006/LocalDrive/actions/runs/40180167303 (success).
- Release metadata confirms exactly one asset: `LocalDrive-1.3.5-Setup-win-x64.exe`, `8,671,232` bytes (~8.27 MiB).
- Published release: https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.6
- GitHub Actions: https://github.com/RaghavSharma006/LocalDrive/actions/runs/40181675054 (success).
- Release metadata confirms exactly one asset: `LocalDrive-1.3.6-Setup-win-x64.exe`, `10,076,160` bytes (~9.61 MiB).
- Previous published release: `v1.3.3`, https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.3.

## Remaining work

1. Publish `v1.3.8` through the existing GitHub release workflow, then user confirms a fresh TeraBox sign-in/mount plus read/write operations. Setup installs the bundled WinFsp driver only if it is missing; Windows will request administrator approval.
2. If connection fails, inspect `%LOCALAPPDATA%\LocalDrive\connection-status.json` and the privacy-safe `%LOCALAPPDATA%\LocalDrive\mount-failure.json` summary.

## TeraBox connect and mount diagnostics (v1.3.7 released)

- Implementation commit: `bb76963cd1341e6cc7498b05178f310e5df3b053`.
- After in-app sign-in has verified the session, the app now reuses that verified session for the immediate mount instead of issuing a duplicate TeraBox root-list request. Reconnect/startup still verifies a stored session once.
- Mount stderr is buffered only in memory and reduced to a fixed safe category. If the rclone process exits during mount, `mount-failure.json` records the exit code, drive letter, safe category, permissions/cache mode and WinFsp launcher presence. Raw provider output, cookies, URLs and remote names are not written.
- Account-check errors now distinguish session rejection, HTTP 403, throttling and network failure without saving raw stderr. The UI no longer tells users to install WinFsp for every engine exit.
- Verification: .NET 10.0.402 publish succeeded; `--verify-file-ops` exited 0; `git diff --check` passed.
- Live account check was intermittent: `--diagnose-connection` passed once, but an immediate UI auto-connect attempt failed at `listing-terabox-root` with the sanitized `terabox-session-rejected` category. No rclone mount process started, no drive was mounted, and no live read/write/mount regression test is claimed. This establishes a current provider-session failure before WinFsp, while the supplied screenshot captures a separate earlier mount-stage failure.
- Deployment: https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.7. GitHub Actions run `40182767493` completed successfully. Release has exactly one asset: `LocalDrive-1.3.7-Setup-win-x64.exe` (10,076,160 bytes).
- Remaining: user validates a fresh sign-in and live mount. My live saved-session checks were intermittent and did not reach WinFsp. If the mount engine exits, inspect the sanitized `mount-failure.json` category and continue from that specific failure.

## TeraBox mount startup follow-up (v1.3.8 in progress)

- Implementation commit: `05b3d33132cb1c4bc8eec9af0b77f94344552d19`.
- Read the user's v1.3.7 diagnostics: drive `T:`, rclone exit code `1`, category `unclassified-mount-error`, and WinFsp launcher present. The saved-session root check later failed before mount, so no authenticated current mount reproduction was available.
- Isolated WinFsp: full rclone 1.75.2-extra and a compact-equivalent `CGO_ENABLED=0`, `cmount` build with the local backend both mounted and read a local folder on temporary `Y:`. The shipped compact engine intentionally omits the local backend; this test build added it solely for the driver check. Both mounts were unmounted after the read.
- Source tracing found separate TeraBox API checks during sign-in/preflight and another when rclone starts the mount. Version 1.3.8 removes the explicit preflight requests so the actual mount performs the auth check once; mount failure now writes only allowlisted diagnostic words and standard OS codes, never raw stderr.
- Verification: .NET publish succeeded, `--verify-file-ops` exited 0, and `git diff --check` passed. Live TeraBox mount/read/write remains unverified because the saved session was rejected before the mount during the latest retry.
- Deployment: pending. Next: checkpoint commit, push source, tag `v1.3.8`, verify CI and the single setup EXE, then request one fresh sign-in/mount validation.
