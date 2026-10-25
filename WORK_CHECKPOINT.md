# LocalDrive checkpoint

Updated 25 October 2026. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Current implementation

- Version: `1.3.4`
- Implementation commit: `59edc962c821a455c8369c1f3d44bbd87ec5729a` (local `main`, not pushed yet).
- Release purpose: build and distribute a compact, TeraBox-only rclone engine while keeping the one-click app downloads small.

## Completed

- Added `Build-SlimRclone.ps1`. It clones rclone-extra tag `v1.75.2-extra`, verifies source commit `56bf13885494819bc801302ddf881279bdac7262`, strips unused backend/command registrations and the optional plugin registration, then builds Windows x64 with `CGO_ENABLED=0`, `cmount`, `-trimpath`, and stripped debug data.
- Engine retains TeraBox, `mount`/Windows `cmount`, `lsjson`, `version`, and built-in help/completion. Setup also includes the rclone MIT license.
- Updated tagged GitHub releases to build and attach `LocalDrive-Rclone-<version>-win-x64.zip`; setup embeds that release URL and the exact archive SHA-256.
- Updated installer to checksum-verify the compact archive, stage/swap its executable with rollback on failure, and replace older unmarked full-size engines. Existing TeraBox session and cache are not altered.
- Bumped the app to `1.3.4`; updated README and third-party notices.

## Verification

- Shipped engine before: `84,690,432` bytes. Compact executable: `20,217,344` bytes (~76.1% smaller).
- Compact engine ZIP: `7,993,714` bytes (7.62 MiB), SHA-256 `AB73534DF3DE2D81685B0253940865DF7F3D9155EBD8166C76503D28FA1A8D02`.
- Compact executable SHA-256: `052DDDC60BF85B20D836A0631914509B72C3A1EDDC4342D733117F36DC6E9797`.
- LocalDrive setup ZIP: `501,457` bytes; one-click setup EXE: `606,208` bytes. Both exclude the engine/runtime installers.
- `Build-SlimRclone.ps1` succeeded and its command smoke checks passed for TeraBox, mount, lsjson, and version. Extracted archive contained only `rclone.exe` and `COPYING`.
- `Build-Standalone.ps1` completed .NET publish and setup packaging. All PowerShell scripts parsed; generated installer URL and archive digest matched; `git diff --check` passed.
- REA opened both original and compact native files as x64 Windows PE. Safe artifact inventory of the final compact engine verified a single PE payload of `20,217,344` bytes with the expected SHA. Ghidra full function/string overviews timed out at 300 seconds for both Go binaries; no complete decompilation is claimed.
- No live TeraBox session, mount/unmount, remote read/write/delete, or executable-from-T: test was run. Those require a Windows user session and authenticated account.

## Deployment

- Pending push of `59edc962c821a455c8369c1f3d44bbd87ec5729a`, tag `v1.3.4`, and successful GitHub Actions release verification.
- Previous published release: `v1.3.3`, https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.3.

## Remaining work and exact next steps

1. Commit this checkpoint update and push both commits to `origin/main`.
2. Push tag `v1.3.4` to trigger the existing Windows release workflow.
3. Verify workflow success and confirm the setup EXE, setup ZIP, and compact rclone ZIP are attached to the release; update this checkpoint with the resulting commit hash and release URL.
4. If workflow fails, inspect its diagnostics and fix before reporting completion.
