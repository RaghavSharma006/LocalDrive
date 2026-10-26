# Changelog

## 1.4.1

- Reapply theme colors after the WPF visual tree loads so dark mode styles the cards and their child controls correctly.
- Restore the existing notification-area window when LocalDrive is launched again, and preserve the real status message after reopening.
- Add a `--preview-ui` mode for checking settings without signing in or mounting a drive.
- Update the user guide and feature status with the preview and tray restore behavior.

## 1.4.0

- Stop bundling the WinFsp MSI in the setup EXE. Setup downloads the official installer only when WinFsp is absent and verifies its pinned hash and publisher signature before installation.
- Remove internal checkpoint and redundant project-brief files from the current GitHub tree; keep the developer checkpoint only in the local working copy.
- Clarify setup downloads and prerequisites in the user documentation.

## 1.3.9

- Improve light/dark theme contrast for secondary text, control borders, hover, selection, and disabled states.
- Clarify cache behavior, sign-in limitations, security/privacy, build steps, and the current verification boundary in the documentation.
- Add a security reporting policy and document that LocalDrive source currently has no license grant.

## 1.3.8

- Remove redundant TeraBox account preflight requests so the mount performs the account check once.
- Keep mount failure diagnostics to a safe category and standard error codes; do not write raw engine output.

See [GitHub Releases](https://github.com/RaghavSharma006/LocalDrive/releases) for downloadable installers and earlier release notes.
