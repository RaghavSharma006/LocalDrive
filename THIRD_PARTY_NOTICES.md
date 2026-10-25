# Third-party components

The single-file setup embeds the compact TeraBox engine. It downloads signed Windows runtimes only when required and verifies them before installation.

## rclone-extra

- Version: `v1.75.2-extra`
- Source and release: https://github.com/gulp79/rclone-extra
- Source revision: `56bf13885494819bc801302ddf881279bdac7262`
- LocalDrive builds a Windows x64 engine with only the TeraBox backend and the `mount`, `lsjson`, and `version` commands. Its SHA-256 is embedded in and verified by the release setup.
- License: MIT; see [`licenses/rclone-extra-COPYING`](licenses/rclone-extra-COPYING).
- The TeraBox backend is an unofficial beta integration. It authenticates with the user's browser session cookie; TeraBox may change the service without notice.

## WinFsp

- Version: `2.1.25156`
- Official release: https://github.com/winfsp/winfsp/releases/tag/v2.1
- Installer: `winfsp-2.1.25156.msi`
- SHA-256: `073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A`
- License: GPLv3 with the FLOSS exception; see [`licenses/WinFsp-License.txt`](licenses/WinFsp-License.txt).

## Microsoft .NET Desktop Runtime

- Version family: .NET 10 Windows Desktop Runtime, x64.
- Download: official Microsoft .NET 10 channel installer (`https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe`).
- The installer signature is validated as Microsoft-signed before it runs.
- WPF and Windows Forms desktop apps require the Windows Desktop Runtime; setup downloads it only when missing.

## Microsoft WebView2

- WPF SDK package: `Microsoft.Web.WebView2` version `1.0.2903.40`, MIT license.
- Runtime bootstrapper: official Evergreen bootstrapper from https://developer.microsoft.com/microsoft-edge/webview2/.
- Microsoft permits bundling the small Evergreen bootstrapper with an app; it downloads the runtime only when setup detects it is missing. The bootstrapper signature is checked by the package script.

## .NET

The desktop app targets Windows x64 and uses Microsoft's .NET 10 Desktop Runtime. Setup downloads and validates the runtime installer if the required runtime is not installed. Microsoft .NET runtime components are MIT licensed.
