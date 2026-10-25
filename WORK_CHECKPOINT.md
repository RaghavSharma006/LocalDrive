# LocalDrive checkpoint

Updated 25 October 2026. Current implementation commit: `e707b4bea8b01f2383477ea2726cbfa519d98494`. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Completed

- Previous v1.3.2 sign-in fix is preserved: cookie capture checks the active and supported TeraBox domains, verifies the account root, and stores the session with Windows DPAPI.
- Added a one-click, self-extracting setup EXE alongside the compact setup ZIP. The EXE extracts its embedded ZIP and runs the installer. The installer reports dependency/download progress and derives its installed display version from the app binary.
- Package keeps the large TeraBox engine and WinFsp MSI out of initial downloads. Setup downloads the pinned rclone-extra release, extracts only `rclone.exe`, and installs WinFsp only if missing.
- Updated README, feature status, GitHub release workflow, and `.gitignore` for both setup assets.
- Installed official Ghidra 12.1.4 under `D:\Tools\ghidra_12.1.4_PUBLIC\ghidra_12.1.4_PUBLIC`, verified SHA-256 `ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db`, and configured `GHIDRA_INSTALL_DIR`/Java 21 in user environment and the REA MCP section of `%USERPROFILE%\.codex\config.toml`.
- Removed the downloaded Ghidra ZIP after verified extraction to reclaim about 570 MB. No REA or Ghidra files were added to the project.

## Verification

- `Build-Standalone.ps1`: successful .NET publish and packaging; output ZIP is 501,098 bytes and one-click EXE is 606,208 bytes.
- Extracted the EXE in IExpress extraction-only mode, then expanded the embedded ZIP. Confirmed the app package has no rclone engine or WinFsp MSI and does not retain the SFX bootstrap script.
- PowerShell parser accepted `Build-Standalone.ps1`, `Install-LocalDrive.ps1`, and `Bootstrap-LocalDrive.ps1`; `git diff --check` passed.
- Ghidra `analyzeHeadless.bat` starts under the IntelliJ Java 21 runtime and prints usage. The already-running REA MCP process still sees its pre-change environment; native REA analysis has not run yet and needs Codex/REA restart.
- Full setup/install, TeraBox sign-in/mount, filesystem read/write, and running programs from T: were not executed as part of package verification.

## Deployment

- Implementation commit `e707b4bea8b01f2383477ea2726cbfa519d98494` and checkpoint commit `c1ef77d84865f1f0febeba29103d2b548924621a` were pushed to `main`; tag `v1.3.3` was pushed.
- GitHub Actions run `40177341632` completed successfully. Published https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.3 with `LocalDrive-1.3.3-Setup-win-x64.exe` (598,016 bytes) and `LocalDrive-1.3.3-Setup-win-x64.zip` (501,326 bytes).
- The setup EXE is unsigned and may trigger SmartScreen's unknown-publisher warning; the ZIP remains available.

## Remaining work and exact next steps

1. Restart Codex to reload the REA MCP environment, then run REA against the shipped rclone binary. The WinFsp MSI is a package container and is not directly accepted as a native binary by REA; inventory its installed/runtime payload if analysis is still needed.
2. If correcting any source after release, build a new patch version and publish it under a new tag; do not replace the existing v1.3.3 release assets silently.
