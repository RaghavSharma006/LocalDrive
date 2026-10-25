# LocalDrive checkpoint

Updated 25 October 2026. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Current implementation

- Version: `1.3.4`
- Implementation commit: `59edc962c821a455c8369c1f3d44bbd87ec5729a`.
- Checkpoint commit before release: `f5e22b2d3df26d458493a8b414e605860759b162`.
- Release purpose: build and distribute a compact, TeraBox-only rclone engine while keeping the one-click app downloads small.

## Completed

- Added `Build-SlimRclone.ps1`. It clones rclone-extra tag `v1.75.2-extra`, verifies source commit `56bf13885494819bc801302ddf881279bdac7262`, strips unused backend/command registrations and optional plugin registration, then builds Windows x64 with `CGO_ENABLED=0`, `cmount`, `-trimpath`, and stripped debug data.
- Engine retains TeraBox, `mount`/Windows `cmount`, `lsjson`, `version`, and built-in help/completion. Setup includes the rclone MIT license.
- Updated tagged GitHub releases to build and attach `LocalDrive-Rclone-<version>-win-x64.zip`; setup embeds that release URL and the exact archive SHA-256.
- Updated installer to checksum-verify the compact archive, stage/swap its executable with rollback on failure, and replace older unmarked full-size engines. Existing TeraBox session and cache are not altered.
- Bumped the app to `1.3.4`; updated README and third-party notices.

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
- Previous published release: `v1.3.3`, https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.3.

## Remaining work

1. User should install `v1.3.4` and confirm TeraBox sign-in/mount plus read/write operations in their Windows session. The installer will replace the older engine after checksum verification.
2. If a live account test fails, inspect `LocalDrive` connection diagnostics and reproduce with the new engine; do not infer success from packaging/CLI checks alone.
