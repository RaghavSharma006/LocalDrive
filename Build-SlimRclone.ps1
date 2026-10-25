param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$sourceTag = 'v1.75.2-extra'
$sourceCommit = '56bf13885494819bc801302ddf881279bdac7262'
$sourceRepository = 'https://github.com/gulp79/rclone-extra.git'
$tempRoot = Join-Path $env:TEMP ('LocalDrive-rclone-build-' + [Guid]::NewGuid().ToString('N'))
$sourcePath = Join-Path $tempRoot 'source'
$stagePath = Join-Path $tempRoot 'package'
$binaryPath = Join-Path $stagePath 'rclone.exe'
$archivePath = [IO.Path]::GetFullPath($OutputPath)
$archiveParent = Split-Path -Parent $archivePath

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git is required to fetch the pinned rclone-extra source.' }
if (-not (Get-Command go -ErrorAction SilentlyContinue)) { throw 'Go 1.26.0 or newer is required to build the compact TeraBox engine.' }
$goVersionText = & go version
if ($goVersionText -notmatch '\bgo([0-9]+\.[0-9]+\.[0-9]+)\b' -or [version]$Matches[1] -lt [version]'1.26.0') {
    throw "Go 1.26.0 or newer is required. Found: $goVersionText"
}
New-Item -ItemType Directory -Force -Path $tempRoot,$archiveParent | Out-Null

try {
    & git clone --quiet --depth 1 --branch $sourceTag $sourceRepository $sourcePath
    if ($LASTEXITCODE -ne 0) { throw 'Could not fetch the pinned rclone-extra source.' }
    $actualCommit = (& git -C $sourcePath rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $sourceCommit) { throw "Unexpected rclone-extra source revision: $actualCommit" }

    @'
// Package all imports only the TeraBox backend used by LocalDrive.
package all

import (
	_ "github.com/rclone/rclone/backend/terabox"
)
'@ | Set-Content -LiteralPath (Join-Path $sourcePath 'backend\all\all.go') -Encoding utf8

    @'
// Package all imports only commands used by LocalDrive.
package all

import (
	_ "github.com/rclone/rclone/cmd"
	_ "github.com/rclone/rclone/cmd/cmount"
	_ "github.com/rclone/rclone/cmd/lsjson"
	_ "github.com/rclone/rclone/cmd/mount"
	_ "github.com/rclone/rclone/cmd/version"
)
'@ | Set-Content -LiteralPath (Join-Path $sourcePath 'cmd\all\all.go') -Encoding utf8

    $mainPath = Join-Path $sourcePath 'rclone.go'
    $mainSource = Get-Content -LiteralPath $mainPath -Raw
    $pluginImport = '(?m)^[\t ]*_ "github\.com/rclone/rclone/lib/plugin"\s*// import plugins\r?\n'
    if ($mainSource -notmatch $pluginImport) { throw 'The pinned rclone main package changed; review its plugin imports before rebuilding.' }
    $mainSource = [regex]::Replace($mainSource, $pluginImport, '')
    Set-Content -LiteralPath $mainPath -Value $mainSource -Encoding utf8

    New-Item -ItemType Directory -Force -Path $stagePath | Out-Null
    $oldGoos = $env:GOOS; $oldGoarch = $env:GOARCH; $oldCgo = $env:CGO_ENABLED
    Push-Location $sourcePath
    try {
        $env:GOOS = 'windows'; $env:GOARCH = 'amd64'; $env:CGO_ENABLED = '0'
        & go build -tags cmount -trimpath -buildvcs=false -ldflags='-s -w' -o $binaryPath .
        if ($LASTEXITCODE -ne 0) { throw "Compact rclone build failed (exit code $LASTEXITCODE)." }
    }
    finally {
        Pop-Location
        $env:GOOS = $oldGoos; $env:GOARCH = $oldGoarch; $env:CGO_ENABLED = $oldCgo
    }

    & $binaryPath help backend terabox *> $null
    if ($LASTEXITCODE -ne 0) { throw 'The compact engine did not register the TeraBox backend.' }
    & $binaryPath mount --help *> $null
    if ($LASTEXITCODE -ne 0) { throw 'The compact engine did not register the Windows mount command.' }
    & $binaryPath lsjson --help *> $null
    if ($LASTEXITCODE -ne 0) { throw 'The compact engine did not register lsjson.' }
    & $binaryPath version *> $null
    if ($LASTEXITCODE -ne 0) { throw 'The compact engine version diagnostic failed.' }

    Copy-Item -LiteralPath (Join-Path $sourcePath 'COPYING') -Destination (Join-Path $stagePath 'COPYING')
    if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
    Compress-Archive -Path (Join-Path $stagePath '*') -DestinationPath $archivePath -CompressionLevel Optimal
    $archive = Get-Item -LiteralPath $archivePath
    $sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    Write-Host ("Compact TeraBox engine package: {0:N2} MiB" -f ($archive.Length / 1MB))
    Write-Host ("SHA-256: {0}" -f $sha256)
    Write-Output $sha256
}
finally {
    $tempFull = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $targetFull = [IO.Path]::GetFullPath($tempRoot)
    if ($targetFull.StartsWith($tempFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $targetFull)) {
        Remove-Item -LiteralPath $targetFull -Recurse -Force
    }
}
