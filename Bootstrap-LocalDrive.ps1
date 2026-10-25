$ErrorActionPreference = 'Stop'
$ProgressPreference = 'Continue'
$archive = Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'LocalDrive-*-Setup-win-x64.zip' -File | Select-Object -First 1
if (-not $archive) { throw 'The LocalDrive setup archive is missing from this package.' }

$extract = Join-Path $env:TEMP ('LocalDrive-package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $extract | Out-Null
$completed = $false
try {
    Write-Host 'Preparing LocalDrive setup...'
    Expand-Archive -LiteralPath $archive.FullName -DestinationPath $extract
    $installer = Join-Path $extract 'Install-LocalDrive.ps1'
    if (-not (Test-Path -LiteralPath $installer)) { throw 'The setup archive is incomplete.' }
    & $installer
    $completed = $true
}
catch {
    Write-Host ''
    Write-Host ('LocalDrive setup failed: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host ('Setup files were kept at: ' + $extract)
    Write-Host 'Press Enter to close this window.'
    [void](Read-Host)
    exit 1
}
finally {
    if ($completed -and (Test-Path -LiteralPath $extract)) {
        Remove-Item -LiteralPath $extract -Recurse -Force
    }
}
