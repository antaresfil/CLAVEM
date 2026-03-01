# Changelog
All notable changes to this project will be documented in this file.

The format is based on **Keep a Changelog**, and this project follows **Semantic Versioning**.

## [2.0.2] - 2026-02-27


### Publisher
- Publisher (Microsoft Store): **Antaresfil**

### Changed

- Project renamed from **previous name** to **CLAVEM - File Encryption** for Microsoft Store publication
- New logo and icon (transparent background)
- Developer: Massimo Parisi (antaresfil)
- Contact: clavemhelp@noxfarm.com
- Support: clavemhelp@noxfarm.com
- Updated all documentation, interface and metadata with new identity

### Security / reliability fixes

- **Fixed master-key zeroing bug**: `DeriveMasterKey` no longer returns a buffer that is later zeroed in-place; returned material is cloned before cleanup.
- **Fixed integer overflow on payload sizing**: explicit bounds checks and `checked` arithmetic prevent silent overflows (e.g., near 2 GB).
- **Fixed infinite loop in `SecureDelete` on 0-byte files**: empty files are deleted immediately.
- **Fixed crash in metadata reading** when an encrypted file disappears mid-read (`FileNotFoundException`, `IOException`, `UnauthorizedAccessException` now handled).
- **Fixed duplicate error dialogs**: centralized UI error reporting to avoid double popups.
- **Refactor**: removed unused `usedKeyfile` parameter from `LogEncryptionSuccess` to reduce ambiguity in logs.

### Notes

- No breaking changes.
- No algorithm changes (AES-256-GCM + Argon2id).
- SVLT container format remains **v3** (compatible with v1/v2/v3 legacy containers).

---

## [2.0.1] - 2026-02-18

### Security / reliability fixes

- **Fixed master-key zeroing bug**: `DeriveMasterKey` no longer returns a buffer that is later zeroed in-place; returned material is cloned before cleanup.
- **Fixed integer overflow on payload sizing**: explicit bounds checks and `checked` arithmetic prevent silent overflows (e.g., near 2 GB).
- **Fixed infinite loop in `SecureDelete` on 0-byte files**: empty files are deleted immediately.
- **Fixed crash in metadata reading** when an encrypted file disappears mid-read (`FileNotFoundException`, `IOException`, `UnauthorizedAccessException` now handled).
- **Fixed duplicate error dialogs**: centralized UI error reporting to avoid double popups.
- **Refactor**: removed unused `usedKeyfile` parameter from `LogEncryptionSuccess` to reduce ambiguity in logs.

### Notes

- No breaking changes.
- No algorithm changes (AES-256-GCM + Argon2id).
- SVLT container format remains **v2** (legacy filename-in-header).
