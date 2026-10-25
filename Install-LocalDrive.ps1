$ErrorActionPreference = 'Stop'
$installRoot = Join-Path $env:LOCALAPPDATA 'Programs\LocalDrive'
$sourceRoot = (Resolve-Path $PSScriptRoot).Path.TrimEnd('\')
$installFull = [IO.Path]::GetFullPath($installRoot).TrimEnd('\')
$runningApp = Get-Process -Name 'LocalDrive' -ErrorAction SilentlyContinue
if ($runningApp) { throw 'Close LocalDrive from its tray menu first. This safely disconnects T: and avoids replacing files while they are in use. Then run setup again.' }

if (-not $sourceRoot.Equals($installFull, [StringComparison]::OrdinalIgnoreCase)) {
    New-Item -ItemType Directory -Force -Path $installFull | Out-Null
    Get-ChildItem -LiteralPath $sourceRoot -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $installFull -Recurse -Force
    }
}

$winFspLauncher = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'WinFsp\bin\launcher-x64.exe'
if (-not (Test-Path -LiteralPath $winFspLauncher)) {
    $msi = Join-Path $installFull 'Prerequisites\winfsp-2.1.25156.msi'
    if (-not (Test-Path -LiteralPath $msi)) { throw 'Bundled WinFsp installer is missing.' }
    $arguments = "/i `"$msi`" /passive /norestart"
    $result = Start-Process -FilePath 'msiexec.exe' -ArgumentList $arguments -Verb RunAs -Wait -PassThru
    if ($result.ExitCode -notin @(0, 3010)) { throw "WinFsp setup failed (exit code $($result.ExitCode))." }
}

$webViewRoot = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft\EdgeWebView\Application'
$hasWebView = (Test-Path -LiteralPath $webViewRoot) -and
    ((Get-ChildItem -LiteralPath $webViewRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d+\.' }).Count -gt 0)
if (-not $hasWebView) {
    $bootstrapper = Join-Path $installFull 'Prerequisites\MicrosoftEdgeWebView2Setup.exe'
    if (-not (Test-Path -LiteralPath $bootstrapper)) { throw 'Bundled Microsoft WebView2 installer is missing.' }
    $result = Start-Process -FilePath $bootstrapper -ArgumentList '/silent', '/install' -Wait -PassThru
    if ($result.ExitCode -ne 0) { throw "WebView2 setup failed (exit code $($result.ExitCode))." }
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
New-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value '1.3.0' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name Publisher -Value 'LocalDrive' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installFull -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value (Join-Path $installFull 'LocalDrive.exe') -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name UninstallString -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstall`"" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
Start-Process -FilePath $shortcut.TargetPath -WorkingDirectory $installFull
Write-Host 'LocalDrive installed. Find it in the Start menu. Its settings and sign-in remain in your Windows user profile.'
