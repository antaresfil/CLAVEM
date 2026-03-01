// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;

namespace CLAVEM.Core
{
    /// <summary>
    /// Minimal, privacy-preserving metadata returned by ReadFileMetadata().
    /// Intentionally does NOT reveal whether a keyfile was used.
    /// </summary>
    public sealed class FileMetadata
    {
        public bool IsValid { get; set; }

        /// <summary>
        /// CLAVEM container version (1 legacy, 2 current).
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Returns a short UI string (privacy-preserving).
        /// </summary>
        public string GetAuthenticationInfo()
        {
            if (!IsValid)
                return "Not a CLAVEM file";

            // Do not disclose keyfile usage.
            return "CLAVEM encrypted file";
        }
    }
}
