@echo off
REM Quick Setup Script for TRS Mod Tool
REM Run this after extracting backup to setup the environment

echo TRS Mod Tool Quick Setup
echo ========================

echo Checking system requirements...

REM Check Windows version
ver | findstr /i "Windows" >nul
if %errorlevel% neq 0 (
    echo ERROR: This tool requires Windows
    pause
    exit /b 1
)

echo ✓ Windows detected

REM Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Not running as Administrator
    echo Some features may not work properly
    echo Please run this script as Administrator if you encounter issues
) else (
    echo ✓ Running as Administrator
)

REM Check for DirectX 9
echo Checking DirectX 9...
if exist "%SystemRoot%\System32\d3d9.dll" (
    echo ✓ DirectX 9 DLL found
) else (
    echo WARNING: DirectX 9 may not be installed
    echo Please install DirectX End-User Runtime from Microsoft
)

REM Check for Visual C++ Redistributables
echo Checking Visual C++ Redistributables...
reg query "HKLM\SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x86" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Visual C++ 2010 x86 found
) else (
    echo WARNING: Visual C++ 2010 x86 not found
)

REM Create necessary directories
echo Creating directories...
if not exist "C:\TRS_ModTool\" mkdir "C:\TRS_ModTool\"
if not exist "C:\TRS_ModTool\Projects\" mkdir "C:\TRS_ModTool\Projects\"
if not exist "C:\TRS_ModTool\Assets\" mkdir "C:\TRS_ModTool\Assets\"
if not exist "C:\TRS_ModTool\Tools\" mkdir "C:\TRS_ModTool\Tools\"
if not exist "C:\TRS_ModTool\Backups\" mkdir "C:\TRS_ModTool\Backups\"
if not exist "C:\Temp\TRS_Temp\" mkdir "C:\Temp\TRS_Temp\"
echo ✓ Directories created

REM Copy configuration file
echo Setting up configuration...
if exist "configs\default_settings.ini" (
    copy "configs\default_settings.ini" "C:\TRS_ModTool\settings.ini" >nul
    echo ✓ Configuration file copied
) else (
    echo WARNING: Configuration file not found
)

echo.
echo Setup complete! 
echo.
echo Next steps:
echo 1. Copy your mod tool files to the created directories
echo 2. Install DirectX 9 End-User Runtime if warned above
echo 3. Install Visual C++ Redistributables if warned above
echo 4. Run your TRS mod tool to verify everything works
echo.
echo For troubleshooting, see: documentation\TROUBLESHOOTING.md
echo For restoration guide, see: documentation\RESTORE_GUIDE.md
echo.

pause