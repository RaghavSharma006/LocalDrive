$ErrorActionPreference = 'Stop'
$ProgressPreference = 'Continue'
$installRoot = Join-Path $env:LOCALAPPDATA 'Programs\LocalDrive'
$sourceRoot = (Resolve-Path $PSScriptRoot).Path.TrimEnd('\')
$installFull = [IO.Path]::GetFullPath($installRoot).TrimEnd('\')
$runningApp = Get-Process -Name 'LocalDrive' -ErrorAction SilentlyContinue
if ($runningApp) { throw 'Close LocalDrive from its tray menu first. This safely disconnects the drive and avoids replacing files while they are in use. Then run setup again.' }

if (-not $sourceRoot.Equals($installFull, [StringComparison]::OrdinalIgnoreCase)) {
    New-Item -ItemType Directory -Force -Path $installFull | Out-Null
    Get-ChildItem -LiteralPath $sourceRoot -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $installFull -Recurse -Force
    }
}

$scratch = Join-Path $env:TEMP ('LocalDrive-setup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
function Download-File([string]$url, [string]$destination) {
    Write-Host ("Downloading {0}..." -f ([Uri]$url).Segments[-1])
    & curl.exe -L --fail --show-error --progress-bar --output $destination $url
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $destination)) { throw "Could not download a required signed dependency from $url" }
}
function Assert-MicrosoftSignature([string]$path) {
    $signature = Get-AuthenticodeSignature -LiteralPath $path
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
        throw "Microsoft signature validation failed for $(Split-Path -Leaf $path)."
    }
}

try {
    $winFspLauncher = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'WinFsp\bin\launcher-x64.exe'
    if (-not (Test-Path -LiteralPath $winFspLauncher)) {
        Write-Host 'WinFsp is missing. Installing the signed filesystem driver; Windows will ask for permission.'
        $msi = Join-Path $scratch 'winfsp-2.1.25156.msi'
        Download-File 'https://github.com/winfsp/winfsp/releases/download/v2.1/winfsp-2.1.25156.msi' $msi
        $actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash
        if ($actual -ne '073A70E00F77423E34BED98B86E600DEF93393BA5822204FAC57A29324DB9F7A') { throw 'WinFsp installer checksum mismatch.' }
        $signature = Get-AuthenticodeSignature -LiteralPath $msi
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'NAVIMATICS') { throw 'WinFsp installer signature validation failed.' }
        $result = Start-Process -FilePath 'msiexec.exe' -ArgumentList @('/i', "`"$msi`"", '/passive', '/norestart') -Verb RunAs -Wait -PassThru
        if ($result.ExitCode -notin @(0, 3010)) { throw "WinFsp setup failed (exit code $($result.ExitCode))." }
    }

    $webViewRoots = @(
        (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft\EdgeWebView\Application'),
        (Join-Path $env:LOCALAPPDATA 'Microsoft\EdgeWebView\Application')
    )
    $hasWebView = $false
    foreach ($root in $webViewRoots) {
        if ((Test-Path -LiteralPath $root) -and (Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d+\.' })) { $hasWebView = $true; break }
    }
    if (-not $hasWebView) {
        Write-Host 'Installing Microsoft WebView2 Runtime...'
        $bootstrapper = Join-Path $scratch 'MicrosoftEdgeWebView2Setup.exe'
        Download-File 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' $bootstrapper
        Assert-MicrosoftSignature $bootstrapper
        $result = Start-Process -FilePath $bootstrapper -ArgumentList '/silent', '/install' -Wait -PassThru
        if ($result.ExitCode -ne 0) { throw "WebView2 setup failed (exit code $($result.ExitCode))." }
    }

    $dotnetRoot = Join-Path $env:ProgramFiles 'dotnet'
    $desktopRuntime = Join-Path $dotnetRoot 'shared\Microsoft.WindowsDesktop.App'
    $hasDesktopRuntime = (Test-Path -LiteralPath $desktopRuntime) -and
        ((Get-ChildItem -LiteralPath $desktopRuntime -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^10\.' }).Count -gt 0)
    if (-not $hasDesktopRuntime) {
        Write-Host 'Installing .NET 10 Desktop Runtime...'
        $runtimeInstaller = Join-Path $scratch 'windowsdesktop-runtime-10-win-x64.exe'
        Download-File 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe' $runtimeInstaller
        Assert-MicrosoftSignature $runtimeInstaller
        $result = Start-Process -FilePath $runtimeInstaller -ArgumentList '/install', '/quiet', '/norestart' -Verb RunAs -Wait -PassThru
        if ($result.ExitCode -notin @(0, 3010)) { throw ".NET Desktop Runtime setup failed (exit code $($result.ExitCode))." }
    }

    $rclone = Join-Path $installFull 'Engine\tools\rclone\rclone.exe'
    if (-not (Test-Path -LiteralPath $rclone)) {
        Write-Host 'Installing the pinned TeraBox drive engine (one-time download)...'
        $rcloneZip = Join-Path $scratch 'rclone-windows-amd64.zip'
        Download-File 'https://github.com/gulp79/rclone-extra/releases/download/v1.75.2-extra/rclone-windows-amd64.zip' $rcloneZip
        $actual = (Get-FileHash -LiteralPath $rcloneZip -Algorithm SHA256).Hash
        if ($actual -ne 'D5A5D9383A607CCB1AF9A871D4CB92906121F32CF111497ADB94958B99315C69') { throw 'The TeraBox engine archive checksum did not match its pinned digest.' }
        $extract = Join-Path $scratch 'rclone'
        Expand-Archive -LiteralPath $rcloneZip -DestinationPath $extract -Force
        $engine = Join-Path $installFull 'Engine\tools\rclone'
        New-Item -ItemType Directory -Force -Path $engine | Out-Null
        $engineBinary = Get-ChildItem -LiteralPath $extract -Filter 'rclone.exe' -File -Recurse | Select-Object -First 1
        if (-not $engineBinary) { throw 'The verified TeraBox engine archive did not contain rclone.exe.' }
        Copy-Item -LiteralPath $engineBinary.FullName -Destination $rclone
        Copy-Item -LiteralPath (Join-Path $installFull 'licenses\rclone-extra-COPYING') -Destination (Join-Path $engine 'COPYING') -Force
    }

    $startMenu = Join-Path ([Environment]::GetFolderPath('Programs')) 'LocalDrive.lnk'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($startMenu)
    $shortcut.TargetPath = Join-Path $installFull 'LocalDrive.exe'
    $shortcut.WorkingDirectory = $installFull
    $shortcut.Description = 'TeraBox virtual drive'
    $shortcut.Save()
    $uninstall = Join-Path $installFull 'Uninstall-LocalDrive.ps1'
    $uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\LocalDrive'
    New-Item -Path $uninstallKey -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name DisplayName -Value 'LocalDrive' -PropertyType String -Force | Out-Null
    $appVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $installFull 'LocalDrive.exe')).ProductVersion
    New-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value $appVersion -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name Publisher -Value 'LocalDrive' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installFull -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value (Join-Path $installFull 'LocalDrive.exe') -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name UninstallString -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstall`"" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
    Start-Process -FilePath $shortcut.TargetPath -WorkingDirectory $installFull
    Write-Host 'LocalDrive installed. Find it in the Start menu. Runtime dependencies were downloaded only if needed.'
}
finally {
    $scratchFull = [IO.Path]::GetFullPath($scratch)
    $tempFull = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($scratchFull.StartsWith($tempFull, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $scratchFull)) {
        Remove-Item -LiteralPath $scratchFull -Recurse -Force
    }
}
