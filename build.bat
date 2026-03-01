@echo off
echo ================================================
echo Clavem - Standard Build Script
echo ================================================
echo.
echo This creates a SMALL executable that requires
echo .NET 8.0 Runtime to be installed on target PC.
echo.
echo For a PORTABLE version (no .NET needed), use:
echo build-portable.bat
echo.
pause

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [1/4] Checking .NET version...
dotnet --version
echo.

echo [2/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages!
    pause
    exit /b 1
)
echo.

echo [3/4] Building Release configuration...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo.

echo [4/4] Build complete!
echo.
echo ================================================
echo SUCCESS! Standard Build Created
echo ================================================
echo.
echo Output location:
echo bin\Release\net8.0-windows\Clavem.exe
echo.
echo File size: ~500 KB
echo.
echo IMPORTANT:
echo This version requires .NET 8.0 Desktop Runtime
echo to be installed on the target PC.
echo.
echo Download Runtime from:
echo https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo For a PORTABLE version that works anywhere:
echo Run: build-portable.bat
echo.
pause
