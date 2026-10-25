$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path $PSScriptRoot).Path
$publish = [IO.Path]::GetFullPath((Join-Path $projectRoot 'release-standalone'))
$projectXml = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'LocalDrive.csproj') -Raw)
$version = $projectXml.Project.PropertyGroup.Version
$archive = Join-Path $projectRoot "LocalDrive-$version-Setup-win-x64.zip"
if (-not $publish.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to publish outside the project folder.'
}
if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }

$workspaceSdk = Join-Path (Split-Path $projectRoot -Parent) '.tools\dotnet\dotnet.exe'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetExe = if (Test-Path -LiteralPath $workspaceSdk) { $workspaceSdk } elseif ($dotnetCommand) { $dotnetCommand.Source } else { throw 'Install the .NET 10 SDK to build LocalDrive.' }
& $dotnetExe publish (Join-Path $projectRoot 'LocalDrive.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$scratch = Join-Path $env:TEMP ('LocalDrive-build-' + [Guid]::NewGuid().ToString('N'))
$engine = Join-Path $publish 'Engine\tools\rclone'
$prerequisites = Join-Path $publish 'Prerequisites'
New-Item -ItemType Directory -Force -Path $scratch,$engine,$prerequisites | Out-Null
try {
    $rcloneZip = Join-Path $scratch 'rclone-windows-amd64.zip'
    & curl.exe -L --fail --silent --show-error --output $rcloneZip 'https://github.com/gulp79/rclone-extra/releases/download/v1.75.2-extra/rclone-windows-amd64.zip'
    if ($LASTEXITCODE -ne 0) { throw 'Could not download the pinned rclone-extra release.' }
    $actual = (Get-FileHash -LiteralPath $rcloneZip -Algorithm SHA256).Hash
    if ($actual -ne 'D5A5D9383A607CCB1AF9A871D4CB92906121F32CF111497ADB94958B99315C69') { throw 'The rclone-extra release checksum did not match the pinned GitHub asset digest.' }
    $rcloneExtract = Join-Path $scratch 'rclone'
    Expand-Archive -LiteralPath $rcloneZip -DestinationPath $rcloneExtract -Force
    $rclone = Get-ChildItem -LiteralPath $rcloneExtract -Filter 'rclone.exe' -File -Recurse | Select-Object -First 1
    if (-not $rclone) { throw 'The rclone-extra archive did not contain rclone.exe.' }
    Copy-Item -LiteralPath $rclone.FullName -Destination (Join-Path $engine 'rclone.exe')
    Copy-Item (Join-Path $projectRoot 'licenses\rclone-extra-COPYING') (Join-Path $engine 'COPYING')

    $winFsp = Join-Path $prerequisites 'winfsp-2.1.25156.msi'
    & curl.exe -L --fail --silent --show-error --output $winFsp 'https://github.com/winfsp/winfsp/releases/download/v2.1/winfsp-2.1.25156.msi'
    if ($LASTEXITCODE -ne 0) { throw 'Could not download the official WinFsp installer.' }
    $actual = (Get-FileHash -LiteralPath $winFsp -Algorithm SHA256).Hash
    if ($actual -ne '073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A') { throw 'WinFsp installer checksum mismatch.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $winFsp
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'NAVIMATICS') { throw 'WinFsp installer signature validation failed.' }

    $webView = Join-Path $prerequisites 'MicrosoftEdgeWebView2Setup.exe'
    & curl.exe -L --fail --silent --show-error --output $webView 'https://go.microsoft.com/fwlink/p/?LinkId=2124703'
    if ($LASTEXITCODE -ne 0) { throw "Could not download Microsoft's WebView2 bootstrapper." }
    $signature = Get-AuthenticodeSignature -LiteralPath $webView
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'WebView2 bootstrapper signature validation failed.' }

    Copy-Item (Join-Path $projectRoot 'README.md') $publish -Force
    Copy-Item (Join-Path $projectRoot 'PROJECT_BRIEF.md') $publish -Force
    Copy-Item (Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md') $publish -Force
    Copy-Item (Join-Path $projectRoot 'licenses\WinFsp-License.txt') $publish -Force
    Copy-Item (Join-Path $projectRoot 'Install-LocalDrive.ps1') $publish -Force
    Copy-Item (Join-Path $projectRoot 'Uninstall-LocalDrive.ps1') $publish -Force
    Copy-Item (Join-Path $projectRoot 'Setup-LocalDrive.cmd') $publish -Force

    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -CompressionLevel Optimal
    Write-Host "Setup package created: $archive"
}
finally {
    $scratchFull = [IO.Path]::GetFullPath($scratch)
    $tempFull = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($scratchFull.StartsWith($tempFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $scratchFull)) {
        Remove-Item -LiteralPath $scratchFull -Recurse -Force
    }
}
