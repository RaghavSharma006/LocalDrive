# LocalDrive checkpoint

Updated 25 October 2026. Sign-in fix source commit/tag: `1908e08` / `v1.3.2`. Repository: https://github.com/RaghavSharma006/LocalDrive (private). No credentials are stored here.

## Completed

- Sign-in now checks the active TeraBox origin first, then supported primary/legacy TeraBox origins. It only accepts a cookie set containing the required session cookie and deduplicates cookie names.
- Added the bare `1024terabox.com` host to supported login hosts.
- If no session cookie is available, the sign-in dialog explains to finish login and return to TeraBox files. It logs only a generic diagnostic stage.
- Connection status distinguishes a verified TeraBox session from a drive setup/mount failure. A missing engine directs users to run setup; early mount exit states the sign-in is valid and suggests WinFsp/drive-letter checks.
- Updated README and feature status, built the 1.3.2 compact setup, and extracted it to `D:\kk\LocalDrive-1.3.2-Compact` without modifying the older folder.
- Pushed `main` and tag `v1.3.2`; `origin/main` matched local commit `1908e08` during verification.
- GitHub Actions run `40175577938` completed successfully, including build, package, release upload, and artifact upload.
- Published https://github.com/RaghavSharma006/LocalDrive/releases/tag/v1.3.2. Setup ZIP is 500,964 bytes; extracted package is about 1.31 MiB.

## Verification

- `D:\clone\.tools\dotnet\dotnet.exe build LocalDrive.csproj -c Release`: succeeded with 0 warnings and 0 errors.
- `Build-Standalone.ps1`: succeeded; local package SHA-256 `6BC733C12CE088A2CE82569EAA0596365CB391110E44BC877D518809E6AA6F04`.
- PowerShell syntax parsing and `git diff --check` passed; the archive contains no rclone engine or Windows runtime installers.
- The currently saved session passed `--diagnose-connection` (exit code 0). At diagnosis time T: was not mounted and there was no rclone mount process. The executable previously in `D:\kk` was version 1.3.0.
- Fresh sign-in with the new supported-domain capture path and live mount have not yet been tested. No cookie values were printed or logged.

## Next step

- Run `D:\kk\LocalDrive-1.3.2-Compact\Setup-LocalDrive.cmd` to install the fixed build. Setup keeps the encrypted session under `%LOCALAPPDATA%\LocalDrive`; after install, retry Connect and confirm the live mount.
