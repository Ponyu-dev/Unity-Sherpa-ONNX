@echo off
REM Reverses setup.cmd:
REM   1. Removes the Samples junction.
REM   2. Re-enables git tracking of Unity-managed files.
REM
REM Use this if you want to commit changes to ProjectSettings or manifest.json.

setlocal

cd /d "%~dp0\.."

echo.
echo === DevProject undo setup ===
echo.

if exist "Assets\Samples\" (
    echo [WORK] Removing junction Assets\Samples
    rmdir "Assets\Samples"
) else (
    echo [skip] No junction to remove
)

pushd ..

call :unskip "DevProject~/Assets/Samples"
call :unskip "DevProject~/Packages/manifest.json"
call :unskip "DevProject~/Packages/packages-lock.json"
call :unskip "DevProject~/ProjectSettings/ProjectVersion.txt"
call :unskip "DevProject~/ProjectSettings/ProjectSettings.asset"
call :unskip "DevProject~/ProjectSettings/AsrSettings.asset"
call :unskip "DevProject~/ProjectSettings/TtsSettings.asset"
call :unskip "DevProject~/ProjectSettings/VadSettings.asset"
call :unskip "DevProject~/ProjectSettings/SherpaOnnxSettings.asset"

popd

echo.
echo === Undo complete ===
echo Run Scripts\setup.cmd to re-enable local development mode.
exit /b 0

:unskip
git update-index --no-skip-worktree %~1 >nul 2>&1
if errorlevel 1 (
    echo   [skip] %~1 ^(not tracked^)
) else (
    echo   [ OK ] %~1
)
goto :eof
