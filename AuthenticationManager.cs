// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CLAVEM.Core
{
    /// <summary>
    /// Password is always required (Option A). Keyfile is optional.
    /// The keyfile factor is based on file CONTENT only (not name/path).
    /// </summary>
    public static class AuthenticationManager
    {
        public static byte[] ReadKeyFileBytes(string keyFilePath)
        {
            if (string.IsNullOrWhiteSpace(keyFilePath))
                throw new ArgumentException("Keyfile path is required.", nameof(keyFilePath));

            return File.ReadAllBytes(keyFilePath);
        }

        public static byte[] DeriveMasterKey(byte[] passwordUtf8Bytes, byte[]? keyFileBytes)
        {
            if (passwordUtf8Bytes == null || passwordUtf8Bytes.Length == 0)
                throw new ArgumentException("Password is required.", nameof(passwordUtf8Bytes));

            byte[] passwordBytes = (byte[])passwordUtf8Bytes.Clone();

            try
            {
                byte[] material;

                if (keyFileBytes != null && keyFileBytes.Length > 0)
                {
                    byte[] keyFileHash = SHA256.HashData(keyFileBytes);

                    material = new byte[passwordBytes.Length + 1 + keyFileHash.Length];
                    Buffer.BlockCopy(passwordBytes, 0, material, 0, passwordBytes.Length);
                    material[passwordBytes.Length] = 0x00;
                    Buffer.BlockCopy(keyFileHash, 0, material, passwordBytes.Length + 1, keyFileHash.Length);

                    CryptographicOperations.ZeroMemory(keyFileHash);
                }
                else
                {
                    material = (byte[])passwordBytes.Clone();
                }

                byte[] result = (byte[])material.Clone();
                CryptographicOperations.ZeroMemory(material);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }

        public static byte[] DeriveMasterKey(string password, byte[]? keyFileBytes)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            try
            {
                byte[] material;

                if (keyFileBytes != null && keyFileBytes.Length > 0)
                {
                    byte[] keyFileHash = SHA256.HashData(keyFileBytes);

                    material = new byte[passwordBytes.Length + 1 + keyFileHash.Length];
                    Buffer.BlockCopy(passwordBytes, 0, material, 0, passwordBytes.Length);
                    material[passwordBytes.Length] = 0x00;
                    Buffer.BlockCopy(keyFileHash, 0, material, passwordBytes.Length + 1, keyFileHash.Length);

                    CryptographicOperations.ZeroMemory(keyFileHash);
                }
                else
                {
                    material = (byte[])passwordBytes.Clone();
                }

                // SECURITY FIX: clone before zeroing.
                // In C#, try { return x; } finally { ZeroMemory(x); } zeroes the
                // same array the caller receives (same heap reference). We must copy
                // the bytes into a new array, zero the original, then return the copy.
                byte[] result = (byte[])material.Clone();
                CryptographicOperations.ZeroMemory(material);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }
}
