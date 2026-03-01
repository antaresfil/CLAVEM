// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace CLAVEM.Core
{
    /// <summary>
    /// Secure cryptography engine using AES-256-GCM with Argon2id key derivation
    /// </summary>
    public class CryptoEngine
    {
        private const int SaltSize = 32; // 256 bits
        private const int NonceSize = 12; // 96 bits for GCM
        private const int TagSize = 16; // 128 bits authentication tag
        private const int KeySize = 32; // 256 bits
        
        // Argon2id parameters (OWASP recommendations)
        private const int Argon2Iterations = 3;
        private const int Argon2MemorySize = 65536; // 64 MB
        private const int Argon2Parallelism = 4;

        /// <summary>
        /// Encrypts a file with multi-factor derived key
        /// </summary>
        
        public static void EncryptFile(string inputPath, string outputPath, byte[] masterKey, bool usedKeyfile)
        {
            // NOTE: CLAVEM no longer stores (or reveals) whether a keyfile was used.
            // If the user chose a keyfile, it must already be incorporated into masterKey by AuthenticationManager.

            // Generate random salt and nonce
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

            // Derive encryption key using Argon2id
            byte[] encryptionKey = DeriveKey(masterKey, salt);

            try
            {
                // Read input file
                // SECURITY FIX: guard against files too large for in-memory processing.
                var inputInfo = new System.IO.FileInfo(inputPath);
                if (inputInfo.Length > int.MaxValue)
                    throw new InvalidOperationException(
                        $"Input file is too large ({inputInfo.Length:N0} bytes). " +
                        "Maximum supported size is 2 GB per file.");
                byte[] fileBytes = File.ReadAllBytes(inputPath);

                // Build plaintext package: [u16 filenameLen][filenameUtf8][fileBytes]
                byte[] filenameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(inputPath));
                if (filenameBytes.Length > ushort.MaxValue)
                    throw new InvalidOperationException("Filename too long.");

                int plaintextLen;
                checked
                {
                    plaintextLen = 2 + filenameBytes.Length + fileBytes.Length;
                }

                byte[] plaintext = new byte[plaintextLen];
                // filenameLen (little-endian)
                ushort fnLen = (ushort)filenameBytes.Length;
                plaintext[0] = (byte)(fnLen & 0xFF);
                plaintext[1] = (byte)((fnLen >> 8) & 0xFF);
                Buffer.BlockCopy(filenameBytes, 0, plaintext, 2, filenameBytes.Length);
                Buffer.BlockCopy(fileBytes, 0, plaintext, 2 + filenameBytes.Length, fileBytes.Length);

                // Encrypt with AES-256-GCM (v3: authenticate header via AAD)
                byte[] ciphertext = new byte[plaintext.Length];
                byte[] tag = new byte[TagSize];

                using (var aes = new AesGcm(encryptionKey, TagSize))
                {
                    byte[] aad = BuildAadV3(salt, nonce);
                    aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
                    CryptographicOperations.ZeroMemory(aad);
                }

                // Determine output path and prevent overwrite
                string finalOutputPath = outputPath;
                if (Directory.Exists(outputPath))
                {
                    finalOutputPath = Path.Combine(outputPath, Path.GetFileName(inputPath) + ".svlt");
                }
                finalOutputPath = GetUniquePath(finalOutputPath);

                // Write encrypted file with header (v3: no cleartext filename; header is authenticated via AAD)
                using (var fs = new FileStream(finalOutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(Encoding.ASCII.GetBytes("SVLT")); // signature
                    bw.Write((byte)3); // Format version 3
                    bw.Write(salt);
                    bw.Write(nonce);
                    bw.Write(tag);
                    bw.Write(ciphertext);
                }

                // Secure cleanup

                CryptographicOperations.ZeroMemory(fileBytes);
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(ciphertext);
                CryptographicOperations.ZeroMemory(filenameBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encryptionKey);
            }
        }

        /// <summary>
        /// Reads metadata from an encrypted file without decrypting it
        /// </summary>
        /// </summary>
        
        public static FileMetadata? ReadFileMetadata(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    // Verify magic header
                    byte[] magic = br.ReadBytes(4);
                    if (magic.Length != 4 || Encoding.ASCII.GetString(magic) != "SVLT")
                        return null; // Not a CLAVEM file

                    byte version = br.ReadByte();
                    if (version != 1 && version != 2 && version != 3)
                        return null;

                    // CLAVEM deliberately does NOT expose whether a keyfile was used.
                    return new FileMetadata
                    {
                        IsValid = true,
                        Version = version
                    };
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// Decrypts a file with the derived key (password + optional keyfile).
        /// CLAVEM does NOT reveal whether a keyfile was used; the user must remember it.
        /// </summary>
        public static void DecryptFile(string inputPath, string outputPath, byte[] masterKey, byte[]? keyFileBytes)
        {
            using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                // Verify magic header
                byte[] magic = br.ReadBytes(4);
                if (magic.Length != 4 || Encoding.ASCII.GetString(magic) != "SVLT")
                    throw new InvalidDataException("Invalid encrypted file format");

                byte version = br.ReadByte();
                if (version != 1 && version != 2 && version != 3)
                    throw new InvalidDataException($"Unsupported file version: {version}");

                // v2 includes a flags byte (ignored for privacy); v1 does not
                if (version == 2)
                {
                    _ = br.ReadByte(); // flags (ignored)
                }

                // Read crypto parameters
                byte[] salt = br.ReadBytes(SaltSize);
                byte[] nonce = br.ReadBytes(NonceSize);
                byte[] tag = br.ReadBytes(TagSize);

                string originalFilename = "";
                if (version <= 2)
                {
                    // v1/v2 store filename in cleartext (legacy). We still sanitize it before use.
                    ushort filenameLength = br.ReadUInt16();
                    byte[] filenameBytes = br.ReadBytes(filenameLength);
                    originalFilename = Encoding.UTF8.GetString(filenameBytes);
                }

                // Read encrypted payload
                // SECURITY FIX: guard against integer overflow for files > 2 GB.
                // Casting a long > int.MaxValue to int silently wraps to a negative number,
                // causing ReadBytes to allocate a wrong-sized buffer and corrupt data.
                long remainingBytes = fs.Length - fs.Position;
                if (remainingBytes > int.MaxValue)
                    throw new InvalidDataException(
                        $"Encrypted payload is too large ({remainingBytes:N0} bytes). " +
                        "Maximum supported size is 2 GB per file.");
                byte[] ciphertext = br.ReadBytes((int)remainingBytes);

                // Derive decryption key
                byte[] decryptionKey = DeriveKey(masterKey, salt);

                try
                {
                    byte[] plaintext = new byte[ciphertext.Length];

                    using (var aes = new AesGcm(decryptionKey, TagSize))
                    {
                        if (version == 3)
                        {
                            byte[] aad = BuildAadV3(salt, nonce);
                            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
                        }
                        else
                        {
                            aes.Decrypt(nonce, ciphertext, tag, plaintext);
                        }
                    }

                    byte[] fileBytesToWrite;
                    if (version == 3)
                    {
                        // Plaintext package: [u16 filenameLen][filenameUtf8][fileBytes]
                        if (plaintext.Length < 2)
                            throw new InvalidDataException("Corrupted encrypted payload");

                        ushort fnLen = BitConverter.ToUInt16(plaintext, 0);
                        if (2 + fnLen > plaintext.Length)
                            throw new InvalidDataException("Corrupted encrypted payload");

                        byte[] fnBytes = new byte[fnLen];
                        Buffer.BlockCopy(plaintext, 2, fnBytes, 0, fnLen);
                        originalFilename = Encoding.UTF8.GetString(fnBytes);

                        int fileLen = plaintext.Length - (2 + fnLen);
                        fileBytesToWrite = new byte[fileLen];
                        Buffer.BlockCopy(plaintext, 2 + fnLen, fileBytesToWrite, 0, fileLen);

                        CryptographicOperations.ZeroMemory(fnBytes);
                    }
                    else
                    {
                        // v1/v2 plaintext is the full file bytes
                        fileBytesToWrite = plaintext;
                    }

                    // Determine output path safely
                    string finalOutputPath = outputPath;
                    if (Directory.Exists(outputPath))
                    {
                        string safeName = SanitizeFilename(originalFilename);
                        if (string.IsNullOrWhiteSpace(safeName))
                        {
                            safeName = Path.GetFileNameWithoutExtension(inputPath);
                            if (string.IsNullOrWhiteSpace(safeName))
                                safeName = "decrypted";
                        }

                        finalOutputPath = Path.Combine(outputPath, safeName);
                        finalOutputPath = EnsurePathIsWithinDirectory(outputPath, finalOutputPath);
                    }

                    finalOutputPath = GetUniquePath(finalOutputPath);

                    // Write decrypted file without overwriting
                    using (var outFs = new FileStream(finalOutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        outFs.Write(fileBytesToWrite, 0, fileBytesToWrite.Length);
                    }

                    // Secure cleanup
                    CryptographicOperations.ZeroMemory(fileBytesToWrite);
                    CryptographicOperations.ZeroMemory(plaintext);
                }
                catch (CryptographicException)
                {
                    // Privacy-preserving message: do NOT reveal if a keyfile was used.
                    throw new UnauthorizedAccessException(@"Decryption failed!

Possible causes:

• Incorrect password
• Missing or wrong keyfile (if you used one)
• File corrupted or tampered

Please verify your inputs and try again.");
}
                finally
                {
                    CryptographicOperations.ZeroMemory(decryptionKey);
                    CryptographicOperations.ZeroMemory(ciphertext);
                }
            }
        }

        /// <summary>
        /// Derives a 256-bit key using Argon2id
        /// </summary>
        /// </summary>
        private static byte[] DeriveKey(byte[] password, byte[] salt)
        {
            using (var argon2 = new Argon2id(password))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = Argon2Parallelism;
                argon2.Iterations = Argon2Iterations;
                argon2.MemorySize = Argon2MemorySize;
                
                return argon2.GetBytes(KeySize);
            }
        }

        
private static byte[] BuildAadV3(byte[] salt, byte[] nonce)
{
    // AAD = magic + version + salt + nonce
    byte[] aad = new byte[4 + 1 + SaltSize + NonceSize];
    Buffer.BlockCopy(Encoding.ASCII.GetBytes("SVLT"), 0, aad, 0, 4);
    aad[4] = 3;
    Buffer.BlockCopy(salt, 0, aad, 5, SaltSize);
    Buffer.BlockCopy(nonce, 0, aad, 5 + SaltSize, NonceSize);
    return aad;
}

private static string SanitizeFilename(string filename)
{
    // Strip any path components
    string name = Path.GetFileName(filename ?? string.Empty);

    // Remove invalid filename chars
    foreach (char c in Path.GetInvalidFileNameChars())
    {
        name = name.Replace(c.ToString(), string.Empty);
    }

    // Prevent empty/whitespace
    name = name.Trim();
    return name;
}

private static string EnsurePathIsWithinDirectory(string baseDirectory, string candidatePath)
{
    string baseFull = Path.GetFullPath(baseDirectory);
    if (!baseFull.EndsWith(Path.DirectorySeparatorChar))
        baseFull += Path.DirectorySeparatorChar;

    string candidateFull = Path.GetFullPath(candidatePath);

    if (!candidateFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
        throw new UnauthorizedAccessException("Invalid output path.");

    return candidateFull;
}

private static string GetUniquePath(string path)
{
    // If path doesn't exist, keep it.
    if (!File.Exists(path) && !Directory.Exists(path))
        return path;

    string directory = Path.GetDirectoryName(path) ?? "";
    string filename = Path.GetFileNameWithoutExtension(path);
    string extension = Path.GetExtension(path);

    // Fallback if no filename
    if (string.IsNullOrWhiteSpace(filename))
        filename = "output";

    for (int i = 1; i < 10000; i++)
    {
        string candidate = Path.Combine(directory, $"{filename} ({i}){extension}");
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
            return candidate;
    }

    throw new IOException("Unable to create a unique output filename.");
}

/// <summary>
        /// Securely deletes original file by overwriting with random data
        /// </summary>
        public static void SecureDelete(string filePath, int passes = 3)
        {
            if (!File.Exists(filePath))
                return;

            var fileInfo = new FileInfo(filePath);
            long fileLength = fileInfo.Length;

            // Empty file: nothing to overwrite (avoid 0-byte buffer infinite loop)
            if (fileLength == 0)
            {
                File.Delete(filePath);
                return;
            }

            // Clamp passes
            if (passes < 1) passes = 1;

            int bufLen = (int)Math.Min(4096L, fileLength);
            byte[] buffer = new byte[bufLen];

            using (var rng = RandomNumberGenerator.Create())
            {
                for (int pass = 0; pass < passes; pass++)
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);

                    long written = 0;
                    while (written < fileLength)
                    {
                        rng.GetBytes(buffer);

                        int toWrite = (int)Math.Min(buffer.Length, fileLength - written);
                        fs.Write(buffer, 0, toWrite);
                        written += toWrite;
                    }

                    fs.Flush(true);
                }
            }

            File.Delete(filePath);
        }
    }
}
