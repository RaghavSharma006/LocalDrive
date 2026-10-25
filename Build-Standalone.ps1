$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path $PSScriptRoot).Path
$publish = [IO.Path]::GetFullPath((Join-Path $projectRoot 'release-online'))
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
& $dotnetExe publish (Join-Path $projectRoot 'LocalDrive.csproj') -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Get-ChildItem -LiteralPath $publish -Filter '*.xml' -File -ErrorAction SilentlyContinue | Remove-Item -Force

$licenses = Join-Path $publish 'licenses'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
foreach ($file in @(
    'README.md',
    'PROJECT_BRIEF.md',
    'THIRD_PARTY_NOTICES.md',
    'Install-LocalDrive.ps1',
    'Uninstall-LocalDrive.ps1',
    'Setup-LocalDrive.cmd'
)) { Copy-Item (Join-Path $projectRoot $file) $publish -Force }
Copy-Item (Join-Path $projectRoot 'licenses\WinFsp-License.txt') $licenses -Force
Copy-Item (Join-Path $projectRoot 'licenses\rclone-extra-COPYING') $licenses -Force

if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -CompressionLevel Optimal
$archiveInfo = Get-Item -LiteralPath $archive
Write-Host ("Compact setup package created: {0} ({1:N2} MB)" -f $archiveInfo.FullName, ($archiveInfo.Length / 1MB))
Write-Host 'The setup downloads pinned, signature-checked runtime dependencies only when they are needed.'
