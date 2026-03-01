# CLAVEM - File Encryption Security Whitepaper

<p align="center">
  <img src="Resources/clavem_big.png" alt="CLAVEM Logo" width="80"/>
</p>
<p align="center"><strong>Massimo Parisi (antaresfil)</strong> — <a href="mailto:clavemhelp@noxfarm.com">clavemhelp@noxfarm.com</a> | Support: <a href="mailto:clavemhelp@noxfarm.com">clavemhelp@noxfarm.com</a></p>

---


## Executive summary

CLAVEM is a Windows desktop application that encrypts files (and optionally folders) using modern, authenticated encryption. The security goal is **confidentiality + integrity** of encrypted content and metadata, with a **password always required** and an optional **keyfile factor**.

This document describes what is **implemented in the current codebase**.

---

## 1. Cryptographic design

### 1.1 Primitives

- **Key derivation (KDF):** Argon2id (via `Konscious.Security.Cryptography.Argon2`)
- **Encryption:** AES-256-GCM (authenticated encryption)
- **Hashing:** SHA-256 (for keyfile content hashing and some internal operations)
- **CSPRNG:** `RandomNumberGenerator` for salts and nonces

### 1.2 Inputs and authentication factors

CLAVEM supports:

- **Password-only** (required)
- **Password + keyfile** (optional)

Keyfile factor is derived from the **file content only**, not its filename or path.

### 1.3 Master key material construction

Let:

- `P` = UTF-8 bytes of the password
- `K` = SHA-256 of the keyfile bytes (only if keyfile is enabled and provided)

Then the master material is:

- **Password-only:** `M = P`
- **Password + keyfile:** `M = P || 0x00 || K`

`M` is then passed into Argon2id together with a random salt to derive the AES-GCM key.

> Note: CLAVEM intentionally does **not** reveal whether a keyfile was used when reading metadata from an encrypted container. Users must remember whether they used a keyfile.

### 1.4 Argon2id parameters

The encryption key is derived using Argon2id with fixed parameters (as implemented in code). Typical values in this branch:

- Memory: **64 MiB**
- Iterations: **3**
- Parallelism: **4**
- Output length: **32 bytes** (AES-256 key)

(See `CryptoEngine.DeriveKey()` for exact values used.)

### 1.5 File format (SVLT v2 container)

The encrypted container stores:

- Magic / version
- Random salt (for Argon2id)
- Random nonce (for AES-GCM)
- Ciphertext + authentication tag

SVLT v2 stores the filename in the header (cleartext). The encrypted payload is protected by AES-256-GCM (confidentiality + integrity).

Note: the header itself is not authenticated as AAD in v2; tampering will typically cause decryption to fail (authentication/tag mismatch), but may still enable denial-of-service scenarios.

The plaintext payload includes the original filename (UTF-8) and file bytes.

---

## 2. Threat model

### 2.1 What CLAVEM defends against

- Offline attackers who obtain `.svlt` files and attempt to decrypt without credentials
- Malicious modification of ciphertext or header fields (detected by AES-GCM authentication)
- Accidental corruption (detected by authentication failure)

### 2.2 What CLAVEM does not claim to defend against

- Compromised endpoint while encrypting/decrypting (keyloggers, malware, memory scraping)
- Attackers with access to RAM snapshots / crash dumps during operation
- Side-channel attacks outside typical desktop threat models

---

## 3. Implementation notes and limitations

### 3.1 Password handling in memory

CLAVEM takes care to avoid keeping plaintext passwords alive longer than necessary. However, on managed runtimes, **absolute guarantees about zeroization cannot be made** (GC movement, copies, JIT behavior). The application uses best-effort techniques (unmanaged BSTR extraction + immediate zeroing, zeroing of derived byte arrays).

For this reason, CLAVEM **does not provide any “password reveal” UI**: showing a password requires creating plaintext strings that cannot be reliably erased.

### 3.2 Folder encryption

When encrypting a folder, CLAVEM currently creates a **temporary ZIP** and then encrypts that ZIP. This can leave recoverable traces on some systems (SSD/NVMe, journaling filesystems, snapshots, TRIM). Users with high-assurance requirements should:
- avoid the folder mode, or
- encrypt on an encrypted volume, and/or
- use OS/vendor secure erase procedures.

### 3.3 File size

The current implementation reads the whole file into memory. Very large files can cause memory pressure or failures. The code includes a hard guard for files larger than 2 GB.

---

## 4. Security disclosures

If you believe you found a vulnerability, please follow the process described in `SECURITY.md`.

