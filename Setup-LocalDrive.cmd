@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-LocalDrive.ps1"
if errorlevel 1 (
  echo.
  echo LocalDrive setup could not finish. Read the error above, then press any key.
  pause >nul
)
