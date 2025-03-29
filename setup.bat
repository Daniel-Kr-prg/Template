@echo off
setlocal enabledelayedexpansion

:: Set console encoding to UTF-8
chcp 65001

:: Warning message before proceeding
echo ===================================================
echo WARNING!
echo By proceeding, this script will OVERWRITE the following:
echo - ProjectSettings
echo - manifest.json
echo - packages-lock.json
echo - Assets\Settings
echo - Assets\AddressableAssetsData
echo.
echo Press ENTER to continue or CTRL+C to abort.
echo ===================================================
pause >nul

:: Set paths
set SYNC_ROOT=.Sync

echo ========================
echo Running Full Template Sync
echo ========================

:: Sync Settings (copy to Assets)
echo Syncing Settings...
xcopy /E /Y /I "%SYNC_ROOT%\Settings" "..\Settings"

:: Sync Addressables (copy to Assets)
echo Syncing AddressableAssetsData...
xcopy /E /Y /I "%SYNC_ROOT%\AddressableAssetsData" "..\AddressableAssetsData"

:: Sync ProjectSettings
echo Syncing ProjectSettings...
IF EXIST "%SYNC_ROOT%\ProjectSettings" (
    for %%F in ("%SYNC_ROOT%\ProjectSettings\*.asset") do (
        copy /Y "%%F" "..\..\ProjectSettings\%%~nxF" >nul
    )
    echo ProjectSettings synced.
) ELSE (
    echo No ProjectSettings to sync.
)

:: Sync manifest.json (Packages folder is on the same level as Assets)
echo Syncing Packages\manifest.json...
IF EXIST "%SYNC_ROOT%\Packages\manifest.json" (
    IF EXIST "..\..\Packages\manifest.json" (
        powershell -ExecutionPolicy Bypass -File ".Sync\sync_manifest.ps1"
    ) ELSE (
        echo Target manifest.json not found.
    )
) ELSE (
    echo Source manifest.json not found.
)

:: Sync packages-lock.json
IF EXIST "%SYNC_ROOT%\Packages\packages-lock.json" (
    copy /Y "%SYNC_ROOT%\Packages\packages-lock.json" "..\..\Packages\packages-lock.json" >nul
    echo packages-lock.json updated.
)

:: Sync .gitignore (adjust the path if you need another level)
IF EXIST ".gitignore" (
    copy /Y "%SYNC_ROOT%\.gitignore" "..\..\TemplateGitignore" >nul
    echo .gitignore synced from template.
) ELSE (
    echo No .gitignore found in template.
)

echo ========================
echo Sync Completed
echo ========================

pause