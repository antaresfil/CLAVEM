# Deployment e Testing Guide

<p align="center">
  <img src="Resources/clavem_big.png" alt="CLAVEM Logo" width="80"/>
</p>
<p align="center"><strong>Massimo Parisi (antaresfil)</strong> — <a href="mailto:clavemhelp@noxfarm.com">clavemhelp@noxfarm.com</a> | Support: <a href="mailto:clavemhelp@noxfarm.com">clavemhelp@noxfarm.com</a></p>

---


## Prerequisiti per lo Sviluppo

### Software Necessario
1. **.NET 8 SDK** (non solo Runtime)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Versione minima: 8.0.0
   - Verifica installazione: `dotnet --version`

2. **Visual Studio 2022** (opzionale ma consigliato)
   - Community Edition (gratuita) o superiore
   - Workload: ".NET Desktop Development"
   - Oppure: Visual Studio Code + C# extension

3. **Windows 10/11** (per eseguire l'app WPF)
   - 64-bit
   - .NET 8 Desktop Runtime installato

### Hardware Opzionale

## Setup del Progetto

### 1. Preparazione

```bash
# Clona o scarica il progetto
cd CLAVEM

# Verifica struttura file
dir
# Dovresti vedere:
# - CLAVEM.sln
# - CLAVEM.csproj
# - *.cs files
# - *.xaml files
# - README.md
```

### 2. Restore delle Dipendenze

```bash
# Restore pacchetti NuGet
dotnet restore

# Output atteso:
# Determining projects to restore...
# Restored CLAVEM.csproj (in XXX ms).
```

### 3. Build del Progetto

```bash
# Build Debug (per sviluppo)
dotnet build --configuration Debug

# Build Release (per distribuzione)
dotnet build --configuration Release

# Output atteso:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

### 4. Esecuzione

```bash
# Run diretto
dotnet run

# Oppure esegui l'exe compilato
cd bin\Release\net8.0-windows
CLAVEM.exe
```

## Testing

### Test Base - Solo Password

1. **Avvia l'applicazione**
2. **Clicca "Browse"** e seleziona un file di test (es. `test.txt`)
3. **Spunta "Use Password"**
4. **Inserisci password**: `TestPassword123!`
5. **Seleziona "Encrypt File"**
6. **Clicca "Execute"**
7. **Verifica**: File `.svlt` creato

**Test Decryption**:
1. **Seleziona il file `.svlt`**
2. **Inserisci stessa password**: `TestPassword123!`
3. **Seleziona "Decrypt File"**
4. **Clicca "Execute"**
5. **Verifica**: File originale ripristinato


2. **Copia il segreto** mostrato nel popup
3. **Apri Google Authenticator** (smartphone)
4. **Aggiungi account** → Inserisci segreto manualmente
5. **Nome**: CLAVEM Test
6. **Segreto**: [quello generato]
7. **Torna all'app** e inserisci il codice a 6 cifre
8. **Procedi con encryption/decryption**


**Prerequisiti**:

**Procedura**:
5. **Procedi con encryption/decryption**

### Test Multi-Fattore

**Combinazione consigliata per test completo**:
- Password: `SecureTest2024!`

**Verifica**:
1. File crittografato richiede TUTTI i fattori per decryption
2. Password sbagliata → Errore

### Test Secure Deletion

1. **Crea file di test** con contenuto riconoscibile
2. **Spunta "Securely delete original file"**
3. **Esegui encryption**
4. **Verifica**: File originale non esiste più
5. **Tenta recovery con tool**: Dovrebbe essere impossibile

## Build per Distribuzione

### Build Standard

```bash
dotnet build --configuration Release
```

Output in: `bin\Release\net8.0-windows\`

### Build Self-Contained (Consigliato)

```bash
dotnet publish --configuration Release \
  --output ./publish \
  --self-contained true \
  --runtime win-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

**Vantaggi**:
- Singolo file .exe
- Include .NET Runtime
- Non richiede .NET installato sul PC target
- Dimensione: ~80-100 MB

**Output**: `publish\CLAVEM.exe`

### Build Script Automatico

Usa il file `build.bat` fornito:

```bash
build.bat
```

Questo script:
1. Verifica .NET SDK
2. Restore pacchetti
3. Build Release
4. Publish self-contained
5. Mostra summary

## Packaging per Distribuzione

### Opzione 1: ZIP Archive

```bash
# Crea cartella distribuzione
mkdir CLAVEM-v1.0

# Copia file necessari
copy publish\CLAVEM.exe CLAVEM-v1.0\
copy README.md CLAVEM-v1.0\
copy GUIDA_RAPIDA_IT.md CLAVEM-v1.0\

# Crea ZIP
# Usa 7-Zip o PowerShell:
Compress-Archive -Path CLAVEM-v1.0 -DestinationPath CLAVEM-v1.0.zip
```

### Opzione 2: Installer (Avanzato)

Usa WiX Toolset o Inno Setup per creare installer .msi o .exe

**WiX esempio**:
```xml
<!-- Richiede WiX Toolset installato -->
<!-- Crea file Product.wxs per installer MSI -->
```

## Troubleshooting Build

### Errore: "SDK not found"
**Soluzione**: Installa .NET 8 SDK da microsoft.com

### Errore: "Package restore failed"
**Soluzione**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
# Retry restore
dotnet restore
```

**Soluzione**:
```bash
# Manually install package
```

### Errore: "WPF not supported"
**Causa**: Stai provando su Linux/Mac
**Soluzione**: Build solo su Windows (WPF richiede Windows)

### Warning: "Platform specific"
**Normale**: WPF è specifico per Windows
**Azione**: Ignora o aggiungi `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` al .csproj

## Verifica Qualità

### Code Quality Checklist

- [ ] Nessun warning nel build
- [ ] Tutti i test passano
- [ ] Nessuna dipendenza mancante
- [ ] File README.md presente
- [ ] Documentazione completa
- [ ] Esempi funzionanti

### Security Checklist

- [ ] Credenziali mai hardcoded
- [ ] Memoria pulita dopo uso (ZeroMemory)
- [ ] Errori non rivelano info sensibili
- [ ] File format validato
- [ ] Random number generation sicuro

### User Experience Checklist

- [ ] UI responsive
- [ ] Messaggi errore chiari
- [ ] Progress indicator durante operations
- [ ] Setup guide accessibile
- [ ] File selection intuitiva
- [ ] Validation in tempo reale

## Continuous Integration (CI)

### GitHub Actions Example

```yaml
# .github/workflows/build.yml
name: Build CLAVEM

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-restore --verbosity normal
```

## Performance Testing

### Benchmark Files

Test con file di diverse dimensioni:
- Small: 1 MB
- Medium: 100 MB
- Large: 1 GB
- XLarge: 10 GB

**Metrica attesa**:
- Encryption speed: 50-100 MB/s
- Decryption speed: 50-100 MB/s
- Memory usage: ~100 MB + file size

### Profiling

Usa Visual Studio Profiler:
1. Debug → Performance Profiler
2. Seleziona "CPU Usage" e "Memory Usage"
3. Start profiling
4. Esegui operazioni test
5. Stop e analizza

## Deployment Environments

### Development
- Windows 11 Dev Machine
- .NET 8 SDK
- Visual Studio 2022

### Testing
- Windows 10/11 Clean VM
- Solo .NET 8 Runtime
- Test tutte le features

### Production
- Windows 10+ (1809 or later)
- Self-contained build
- Nessuna dipendenza esterna

## Maintenance

### Aggiornamenti Consigliati

**Mensile**:
- Update NuGet packages
- Security patches
- Bug fixes

**Trimestrale**:
- .NET SDK updates
- Feature enhancements
- Performance optimizations

**Annuale**:
- Major version release
- Breaking changes (se necessario)
- Crypto library updates

### Versioning Strategy

Usa Semantic Versioning (SemVer):
- MAJOR.MINOR.PATCH
- 1.0.0 → Initial release
- 1.1.0 → New feature
- 1.1.1 → Bug fix

## Support e Community

### Issue Tracking
- GitHub Issues per bug reports
- Feature requests via discussions
- Security issues: private disclosure

### Documentation
- README.md → User guide
- SECURITY_WHITEPAPER.md → Technical details
- GUIDA_RAPIDA_IT.md → Quick start (Italian)

---

**Happy Building! 🚀**

Per domande o problemi, consulta la documentazione completa o apri un issue su GitHub.
