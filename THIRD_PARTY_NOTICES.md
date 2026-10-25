# Third-party components

The build/package script downloads these pinned runtime components. It verifies them before packaging.

## rclone-extra

- Version: `v1.75.2-extra`
- Source and release: https://github.com/gulp79/rclone-extra
- Windows asset: `rclone-windows-amd64.zip`
- SHA-256: `D5A5D9383A607CCB1AF9A871D4CB92906121F32CF111497ADB94958B99315C69`
- License: MIT; see [`licenses/rclone-extra-COPYING`](licenses/rclone-extra-COPYING).
- The TeraBox backend is an unofficial beta integration. It authenticates with the user's browser session cookie; TeraBox may change the service without notice.

## WinFsp

- Version: `2.1.25156`
- Official release: https://github.com/winfsp/winfsp/releases/tag/v2.1
- Installer: `winfsp-2.1.25156.msi`
- SHA-256: `073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A`
- License: GPLv3 with the FLOSS exception; see [`licenses/WinFsp-License.txt`](licenses/WinFsp-License.txt).

## Microsoft WebView2

- WPF SDK package: `Microsoft.Web.WebView2` version `1.0.2903.40`, MIT license.
- Runtime bootstrapper: official Evergreen bootstrapper from https://developer.microsoft.com/microsoft-edge/webview2/.
- Microsoft permits bundling the small Evergreen bootstrapper with an app; it downloads the runtime only when setup detects it is missing. The bootstrapper signature is checked by the package script.

## .NET

The setup archive is published self-contained for Windows x64 using the .NET 10 runtime. Microsoft .NET runtime components are MIT licensed.
