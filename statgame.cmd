@echo off
setlocal DisableDelayedExpansion
set "seaside_build_dir=%~dp0build\FirstScene_Seaside"
set "seaside_game_exe=%seaside_build_dir%\Seaside Whitebox.exe"

if not exist "%seaside_game_exe%" (
    echo Game executable not found:
    echo "%seaside_game_exe%"
    echo Build the game into build\FirstScene_Seaside and try again.
    pause
    exit /b 1
)

if not exist "%seaside_build_dir%\Seaside Whitebox_Data\globalgamemanagers" (
    echo Game data is missing. Keep the complete Unity build folder together.
    pause
    exit /b 1
)

start "" /D "%seaside_build_dir%" "%seaside_game_exe%" %*
if errorlevel 1 (
    echo Failed to launch the game.
    pause
    exit /b 1
)
endlocal
exit /b 0
