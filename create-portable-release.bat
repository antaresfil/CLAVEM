@echo off
echo ================================================
echo Clavem - Portable Release Package Creator
echo ================================================
echo.
echo This will create a PORTABLE RELEASE package
echo for end users (no compilation needed).
echo.
echo Output: Single EXE that works on any Windows 10/11
echo Size: ~100 MB (includes everything)
echo.
pause

echo [1/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK to build the release.
    pause
    exit /b 1
)

echo [2/4] Building portable executable...
echo This may take a few minutes...
echo.
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -p:AssemblyName=ClavemPortable

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo [3/4] Creating release package...

REM Crea cartella per il pacchetto
if exist "Clavem-Portable-v1.0.7" rmdir /s /q "Clavem-Portable-v1.0.7"
mkdir "Clavem-Portable-v1.0.7"

REM Copia l'eseguibile
echo Copying executable...
copy "bin\Release\net8.0-windows\win-x64\publish\ClavemPortable.exe" "Clavem-Portable-v1.0.7\"

REM Crea README per utenti finali
echo Creating user README...
(
echo ==========================================
echo   Clavem v1.0.7 - Portable Edition
echo ==========================================
echo.
echo WHAT IS THIS?
echo Clavem is a multi-factor file encryption system
echo that uses AES-256-GCM encryption to protect your files.
echo.
echo SYSTEM REQUIREMENTS
echo - Windows 10 ^(version 1607 or later^) or Windows 11
echo - 64-bit system
echo - 512 MB RAM minimum
echo - 150 MB free disk space
echo.
echo HOW TO USE
echo 1. Double-click ClavemPortable.exe
echo 2. No installation needed!
echo 3. Click the Help buttons for instructions
echo.
echo FEATURES
echo - AES-256-GCM encryption
echo - Password protection
echo - Optional keyfile support
echo - Secure file deletion
echo - Easy to use interface
echo.
echo PORTABLE
echo This version is completely portable:
echo - No installation required
echo - No .NET Runtime needed
echo - Copy to USB and use anywhere
echo - Works immediately on any compatible PC
echo.
echo FIRST TIME USE
echo 1. Run ClavemPortable.exe
echo 2. Click "Help ^(IT^)" or "Help ^(EN^)" for detailed instructions
echo 3. Click "About" for license information
echo.
echo SUPPORT
echo For questions or commercial licensing:
echo Email: mpsecurevault@noxfarm.com
echo.
echo LICENSE
echo Free for personal use.
echo For commercial use, please contact the developer.
echo.
echo DISCLAIMER
echo This software is provided "AS IS" without warranty.
echo Always keep backups of your important files!
echo.
echo DEVELOPER
echo Massimo Parisi
echo Copyright © 2026 Massimo Parisi. All rights reserved.
echo.
echo ==========================================
) > "Clavem-Portable-v1.0.7\README.txt"

REM Crea quick start guide
echo Creating Quick Start guide...
(
echo ==========================================
echo   QUICK START GUIDE
echo ==========================================
echo.
echo ENCRYPT A FILE
echo 1. Select "Encrypt File"
echo 2. Browse and choose your file
echo 3. Enter a strong password
echo 4. ^(Optional^) Select a keyfile for extra security
echo 5. Click "Execute"
echo 6. Done! Your file is now encrypted ^(.svlt file created^)
echo.
echo DECRYPT A FILE
echo 1. Select "Decrypt File"
echo 2. Browse and choose the .svlt file
echo 3. Enter the same password used for encryption
echo 4. If you used a keyfile, select it
echo 5. Click "Execute"
echo 6. Done! Your file is restored
echo.
echo TIPS
echo - Use STRONG passwords ^(12+ characters^)
echo - Combine password + keyfile for maximum security
echo - Keep your keyfile safe ^(you can't decrypt without it!^)
echo - Always test decrypt before deleting originals
echo - Use "Secure Delete" for sensitive files
echo.
echo For detailed instructions, click the Help buttons
echo inside the program ^(available in Italian and English^).
echo.
) > "Clavem-Portable-v1.0.7\QUICK_START.txt"

echo [4/4] Creating ZIP archive...
powershell -command "Compress-Archive -Path 'Clavem-Portable-v1.0.7' -DestinationPath 'Clavem-Portable-v1.0.7.zip' -Force"

REM Pulisci cartella temporanea
rmdir /s /q "Clavem-Portable-v1.0.7"

echo.
echo ================================================
echo SUCCESS! Portable Release Package Created
echo ================================================
echo.
echo File: Clavem-Portable-v1.0.7.zip
echo Size: ~100 MB
echo.
echo This package contains:
echo - Clavem.exe ^(portable, works anywhere^)
echo - README.txt ^(user instructions^)
echo - QUICK_START.txt ^(quick guide^)
echo.
echo Target audience: End users
echo Requirements: Windows 10/11 ^(no .NET installation needed^)
echo.
echo You can now distribute this ZIP file to users!
echo.
pause
