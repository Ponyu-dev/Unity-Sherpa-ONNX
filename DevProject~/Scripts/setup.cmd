@echo off
REM Sets up DevProject on a fresh Windows machine:
REM   1. Replaces the broken Samples symlink stub with a directory junction.
REM   2. Marks Unity auto-modified files as skip-worktree so local edits
REM      don't pollute git status or risk being committed.
REM
REM Idempotent. Safe to run multiple times.

setlocal enabledelayedexpansion

REM Move to DevProject~ (parent of this script's Scripts/ folder).
cd /d "%~dp0\.."

echo.
echo === DevProject local setup ===
echo.

REM ---------------------------------------------------------------
REM 1. Samples junction
REM ---------------------------------------------------------------
if exist "Assets\Samples\" (
    echo [ OK ] Assets\Samples is already a directory ^(junction or real^).
) else (
    if exist "Assets\Samples" (
        echo [WORK] Removing broken symlink stub at Assets\Samples
        del /q "Assets\Samples"
    )
    echo [WORK] Creating junction Assets\Samples -^> ..\..\Samples~
    mklink /J "Assets\Samples" "..\..\Samples~"
    if errorlevel 1 (
        echo [FAIL] mklink failed. Aborting.
        exit /b 1
    )
)

REM ---------------------------------------------------------------
REM 2. Mark Unity auto-modified files as skip-worktree
REM ---------------------------------------------------------------
echo.
echo [WORK] Marking Unity-managed files as skip-worktree...

pushd ..

call :skip "DevProject~/Assets/Samples"
call :skip "DevProject~/Packages/manifest.json"
call :skip "DevProject~/Packages/packages-lock.json"
call :skip "DevProject~/ProjectSettings/ProjectVersion.txt"
call :skip "DevProject~/ProjectSettings/ProjectSettings.asset"
call :skip "DevProject~/ProjectSettings/AsrSettings.asset"
call :skip "DevProject~/ProjectSettings/TtsSettings.asset"
call :skip "DevProject~/ProjectSettings/VadSettings.asset"
call :skip "DevProject~/ProjectSettings/SherpaOnnxSettings.asset"

popd

echo.
echo === Setup complete ===
echo You can now open DevProject~ in Unity.
echo To undo: run Scripts\unsetup.cmd
exit /b 0

:skip
git update-index --skip-worktree %~1 >nul 2>&1
if errorlevel 1 (
    echo   [skip] %~1 ^(not tracked, nothing to do^)
) else (
    echo   [ OK ] %~1
)
goto :eof
