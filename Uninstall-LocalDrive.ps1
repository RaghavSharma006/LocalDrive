$ErrorActionPreference = 'Stop'
$programsRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs')).TrimEnd('\')
$installRoot = [IO.Path]::GetFullPath((Join-Path $programsRoot 'LocalDrive')).TrimEnd('\')
if (-not ([IO.Path]::GetDirectoryName($installRoot).Equals($programsRoot, [StringComparison]::OrdinalIgnoreCase)) -or
    -not ([IO.Path]::GetFileName($installRoot).Equals('LocalDrive', [StringComparison]::OrdinalIgnoreCase))) {
    throw 'Install folder did not match the expected LocalDrive path.'
}
if (Get-Process -Name 'LocalDrive' -ErrorAction SilentlyContinue) {
    throw 'Exit LocalDrive from its tray menu before uninstalling so T: is safely disconnected.'
}
Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'LocalDrive.lnk') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\LocalDrive' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installRoot -Recurse -Force
Write-Host 'LocalDrive program files were removed. Your sign-in, cache, and settings in %LOCALAPPDATA%\LocalDrive were kept.'
