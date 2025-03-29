@echo off
setlocal enabledelayedexpansion

REM Set paths
set SYNC_ROOT=Assets\Template\.Sync

echo ========================
echo 🔁 Running Full Template Sync
echo ========================

REM Sync Settings
echo 🔧 Syncing Settings...
xcopy /E /Y /I "%SYNC_ROOT%\Settings" "Assets\Settings"

REM Sync Addressables
echo 📦 Syncing Addressables...
xcopy /E /Y /I "%SYNC_ROOT%\AddressableAssetsData" "Assets\AddressableAssetsData"

REM Sync ProjectSettings
echo 🛠 Syncing ProjectSettings...
IF EXIST "%SYNC_ROOT%\ProjectSettings" (
    for %%F in ("%SYNC_ROOT%\ProjectSettings\*.asset") do (
        copy /Y "%%F" "ProjectSettings\%%~nxF" >nul
    )
    echo ✅ ProjectSettings synced.
) ELSE (
    echo ⚠️ No ProjectSettings to sync.
)

REM Sync manifest.json
echo 📄 Syncing Packages/manifest.json...
IF EXIST "%SYNC_ROOT%\Packages\manifest.json" (
    IF EXIST "Packages\manifest.json" (
        powershell -Command ^
        "$src = Get-Content '%SYNC_ROOT%\Packages\manifest.json' -Raw | ConvertFrom-Json;" ^
        "$dst = Get-Content 'Packages\manifest.json' -Raw | ConvertFrom-Json;" ^
        "$changed = $false;" ^
        "foreach ($dep in $src.dependencies.PSObject.Properties) {" ^
        "  if (-not $dst.dependencies.PSObject.Properties.Name.Contains($dep.Name)) {" ^
        "    $dst.dependencies[$dep.Name] = $dep.Value; $changed = $true;" ^
        "    Write-Host \"📦 Added: $($dep.Name) → $($dep.Value)\"" ^
        "  }" ^
        "};" ^
        "if ($changed) { $dst | ConvertTo-Json -Depth 100 | Set-Content 'Packages\manifest.json'; Write-Host '✅ manifest.json updated.' }" ^
        "else { Write-Host '🟢 All packages already present.' }"
    ) ELSE (
        echo ❌ Target manifest.json not found.
    )
) ELSE (
    echo ❌ Source manifest.json not found.
)

REM Sync packages-lock.json
IF EXIST "%SYNC_ROOT%\Packages\packages-lock.json" (
    copy /Y "%SYNC_ROOT%\Packages\packages-lock.json" "Packages\packages-lock.json" >nul
    echo 🔒 packages-lock.json updated.
)

REM Sync .gitignore
IF EXIST "%SYNC_ROOT%\.gitignore" (
    copy /Y "%SYNC_ROOT%\.gitignore" ".gitignore" >nul
    echo 📄 .gitignore synced from template.
) ELSE (
    echo ⚠️ No .gitignore found in template.
)

echo ========================
echo ✅ Sync Completed
echo ========================

pause