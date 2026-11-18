@echo off
setlocal
set "FOREST_EXE=%~dp0Play64\Forest\ForestWhitebox.exe"
if not exist "%FOREST_EXE%" (
  echo Forest build not found. Build with Project6.4\Tools\BuildAndTest.ps1 -ForestWhitebox
  pause
  exit /b 1
)
start "Forest Whitebox" /D "%~dp0Play64\Forest" "%FOREST_EXE%" %*
