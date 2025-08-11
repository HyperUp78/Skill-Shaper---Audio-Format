@echo off
REM TRS Mod Tool Backup Script for Windows
REM Creates compressed backup optimized for mobile storage

echo TRS Mod Tool Backup Creator
echo ==========================

REM Get timestamp
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set "YY=%dt:~2,2%" & set "YYYY=%dt:~0,4%" & set "MM=%dt:~4,2%" & set "DD=%dt:~6,2%"
set "HH=%dt:~8,2%" & set "Min=%dt:~10,2%" & set "Sec=%dt:~12,2%"
set "timestamp=%YYYY%%MM%%DD%_%HH%%Min%%Sec%"

set BACKUP_DIR=TRS_Mod_Tool_Backup
set BACKUP_NAME=trs_mod_backup_%timestamp%

echo Select backup type:
echo 1. Full backup (everything)
echo 2. Essential backup (configs + docs + small samples)
echo 3. Mobile backup (docs + configs only)
set /p choice=Enter choice (1-3): 

if "%choice%"=="1" (
    echo Creating full backup...
    powershell -command "Compress-Archive -Path '%BACKUP_DIR%' -DestinationPath '%BACKUP_NAME%_full.zip'"
    echo Full backup created: %BACKUP_NAME%_full.zip
) else if "%choice%"=="2" (
    echo Creating essential backup...
    powershell -command "Compress-Archive -Path '%BACKUP_DIR%\documentation','%BACKUP_DIR%\configs','%BACKUP_DIR%\samples','%BACKUP_DIR%\backup_scripts','TRS_MOD_TOOL_BACKUP.md' -DestinationPath '%BACKUP_NAME%_essential.zip'"
    echo Essential backup created: %BACKUP_NAME%_essential.zip
) else if "%choice%"=="3" (
    echo Creating mobile backup...
    powershell -command "Compress-Archive -Path '%BACKUP_DIR%\documentation','%BACKUP_DIR%\configs','TRS_MOD_TOOL_BACKUP.md' -DestinationPath '%BACKUP_NAME%_mobile.zip'"
    echo Mobile backup created: %BACKUP_NAME%_mobile.zip
) else (
    echo Invalid choice. Exiting.
    exit /b 1
)

echo.
echo Backup file sizes:
dir *%timestamp%*.zip

echo.
echo Backup complete! Transfer files to your phone via:
echo - USB cable
echo - Cloud storage (Google Drive, Dropbox, etc.)
echo - Email (for smaller backups)
echo - Bluetooth/WiFi Direct

pause