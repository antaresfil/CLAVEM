@echo off
echo ================================================
echo CLAVEM - Portable Build Script
echo ================================================
echo.
echo This will create a self-contained executable
echo that works on ANY Windows 10/11 PC without .NET installed.
echo.
echo Output name: CLAVEM.exe
echo Output size: ~100 MB (includes runtime)
echo.
pause

echo [1/4] Checking .NET version...
dotnet --version
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from Microsoft
    pause
    exit /b 1
)

echo [2/4] Cleaning previous builds...
dotnet clean -c Release

echo [3/4] Building self-contained single-file executable...
echo This may take a few minutes...
echo.

dotnet publish -c Release -r win-x64 ^
--self-contained true ^
-p:PublishSingleFile=true ^
-p:IncludeNativeLibrariesForSelfExtract=true ^
-p:DebugType=none ^
-p:DebugSymbols=false ^
-p:AssemblyName=CLAVEM

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo [4/4] Build complete!
echo.
echo ================================================
echo SUCCESS! Executable Created
echo ================================================
echo.
echo Your executable is located at:
echo bin\Release\net8.0-windows\win-x64\publish\CLAVEM.exe
echo.
echo This EXE can now:
echo   - Run on any Windows 10/11 PC
echo   - No .NET installation required
echo   - Be packaged into MSIX
echo   - Be distributed as portable version
echo.
pause