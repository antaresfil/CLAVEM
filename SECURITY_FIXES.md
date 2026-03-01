# CLAVEM - File Encryption v2.0.2 — Security & reliability fixes

This document summarizes the security-relevant fixes included in **v2.0.2**.

## ✅ Fixed issues

### 1) Master-key zeroing bug (critical)

**Problem:** `DeriveMasterKey` returned a byte[] that was later zeroed in a `finally` block, resulting in an all-zero master key being used for encryption.

**Fix:** Returned material is now **cloned** before zeroing the original buffers, ensuring callers never receive a reference that is later wiped.

---

### 2) Integer overflow / size guards (reliability & safety)

**Problem:** Potential silent overflows (or invalid casts from `long` to `int`) around payload sizing near the **2 GB** limit.

**Fix:** Explicit bounds checks and `checked` arithmetic prevent silent wraparound and surface errors clearly.

---

### 3) SecureDelete infinite loop on 0-byte files

**Problem:** 0-byte files could cause a loop that never progresses.

**Fix:** Empty files are deleted immediately.

---

### 4) Metadata read crash when file disappears

**Problem:** `ReadFileMetadata` could throw if the file is deleted/moved mid-read.

**Fix:** The method now handles `FileNotFoundException`, `IOException`, and `UnauthorizedAccessException` and returns `null`.

---

### 5) Duplicate error dialogs

**Problem:** The same failure could be shown twice (inner catch + outer catch).

**Fix:** Centralized UI error reporting with a single user-facing message.

---

### 6) Logging cleanup

**Change:** Removed unused `usedKeyfile` parameter from `LogEncryptionSuccess` to avoid ambiguity.

## File format

- **Encrypt:** writes **SVLT v2** (legacy format; filename stored in header; payload is AES-GCM encrypted)
- **Decrypt:** supports **SVLT v1/v2** (legacy)

> CLAVEM does not store or reveal whether a keyfile was used.
