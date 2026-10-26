param(
    [string]$RcloneArchivePath,
    [string]$RcloneArchiveSha256
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path $PSScriptRoot).Path
$publish = [IO.Path]::GetFullPath((Join-Path $projectRoot 'release-online'))
$projectXml = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'LocalDrive.csproj') -Raw)
$version = $projectXml.Project.PropertyGroup.Version
$archive = Join-Path $projectRoot "LocalDrive-$version-Setup-win-x64.zip"
$setupExe = Join-Path $projectRoot "LocalDrive-$version-Setup-win-x64.exe"
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
    'CHANGELOG.md',
    'FEATURE_STATUS.md',
    'SECURITY.md',
    'THIRD_PARTY_NOTICES.md',
    'Install-LocalDrive.ps1',
    'Uninstall-LocalDrive.ps1',
    'Setup-LocalDrive.cmd'
)) { Copy-Item (Join-Path $projectRoot $file) $publish -Force }
if ($RcloneArchivePath) {
    if (-not (Test-Path -LiteralPath $RcloneArchivePath -PathType Leaf)) { throw 'The slim rclone archive does not exist.' }
    if ($RcloneArchiveSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'A valid SHA-256 is required for the slim rclone archive.' }
    $archiveDigest = (Get-FileHash -LiteralPath $RcloneArchivePath -Algorithm SHA256).Hash
    if ($archiveDigest -ne $RcloneArchiveSha256.ToUpperInvariant()) { throw 'The supplied slim rclone archive does not match its SHA-256.' }
    $installerPath = Join-Path $publish 'Install-LocalDrive.ps1'
    $installer = Get-Content -LiteralPath $installerPath -Raw
    $installer = $installer.Replace("`$pinnedEngineUrl = 'https://github.com/gulp79/rclone-extra/releases/download/v1.75.2-extra/rclone-windows-amd64.zip'", "`$pinnedEngineUrl = ''")
    $installer = $installer.Replace('D5A5D9383A607CCB1AF9A871D4CB92906121F32CF111497ADB94958B99315C69', $RcloneArchiveSha256.ToUpperInvariant())
    Set-Content -LiteralPath $installerPath -Value $installer -Encoding utf8
    Copy-Item -LiteralPath $RcloneArchivePath -Destination $publish
}
Copy-Item (Join-Path $projectRoot 'licenses\WinFsp-License.txt') $licenses -Force
Copy-Item (Join-Path $projectRoot 'licenses\rclone-extra-COPYING') $licenses -Force

if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive -CompressionLevel Optimal
$archiveInfo = Get-Item -LiteralPath $archive
Write-Host ("Compact setup package created: {0} ({1:N2} MB)" -f $archiveInfo.FullName, ($archiveInfo.Length / 1MB))
Write-Host 'The one-file setup embeds the compact TeraBox engine. WinFsp, .NET Desktop Runtime, and WebView2 download separately during setup only when missing.'

$iexpress = Join-Path $env:WINDIR 'System32\iexpress.exe'
if (-not (Test-Path -LiteralPath $iexpress)) { throw 'IExpress is required to build the one-click Windows setup executable.' }
$stage = Join-Path $env:TEMP ('LocalDrive-iexpress-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    $archiveName = Split-Path -Leaf $archive
    Copy-Item -LiteralPath $archive -Destination (Join-Path $stage $archiveName)
    Copy-Item -LiteralPath (Join-Path $projectRoot 'Bootstrap-LocalDrive.ps1') -Destination $stage
    $sedPath = Join-Path $stage 'LocalDrive.sed'
    $sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$setupExe
FriendlyName=LocalDrive Setup
AppLaunched=powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File Bootstrap-LocalDrive.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="Bootstrap-LocalDrive.ps1"
FILE1="$archiveName"
[SourceFiles]
SourceFiles0=$stage\
[SourceFiles0]
%FILE0%=
%FILE1%=
"@
    Set-Content -LiteralPath $sedPath -Value $sed -Encoding Default
    if (Test-Path -LiteralPath $setupExe) { Remove-Item -LiteralPath $setupExe -Force }
    & $env:ComSpec /d /c "$iexpress /N $sedPath"
    $iexpressExit = $LASTEXITCODE
    if ($iexpressExit -ne 0 -or -not (Test-Path -LiteralPath $setupExe)) { throw "IExpress did not create the one-click setup executable (exit code $iexpressExit)." }
    $setupInfo = Get-Item -LiteralPath $setupExe
    Write-Host ("One-click setup executable created: {0} ({1:N2} MB)" -f $setupInfo.FullName, ($setupInfo.Length / 1MB))
}
finally {
    $stageFull = [IO.Path]::GetFullPath($stage)
    $tempFull = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($stageFull.StartsWith($tempFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $stageFull)) {
        Remove-Item -LiteralPath $stageFull -Recurse -Force
    }
}
