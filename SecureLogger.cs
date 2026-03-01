// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.IO;

namespace CLAVEM.Core
{
    /// <summary>
    /// Secure logging system that logs security events without sensitive data
    /// </summary>
    public static class SecureLogger
    {
        private static readonly object _lockObj = new object();
        private static string? _logPath = null;

        /// <summary>
        /// Logs a security event (without sensitive data like passwords or keys)
        /// </summary>
        public static void LogSecurityEvent(string eventType, string details)
        {
            try
            {
                lock (_lockObj)
                {
                    string logPath = GetSecureLogPath();
                    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    string logEntry = $"[{timestamp} UTC] {eventType}: {details}";

                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Logging should never crash the application
            }
        }

        /// <summary>
        /// Logs successful encryption
        /// </summary>
        public static void LogEncryptionSuccess(string filename, long fileSize)
        {
            LogSecurityEvent("ENCRYPT_SUCCESS",
                $"File: {SanitizeFilename(filename)}, " +
                $"Size: {fileSize} bytes");
        }

        /// <summary>
        /// Logs successful decryption
        /// </summary>
        public static void LogDecryptionSuccess(string filename)
        {
            LogSecurityEvent("DECRYPT_SUCCESS", 
                $"File: {SanitizeFilename(filename)}");
        }

        /// <summary>
        /// Logs failed decryption attempt
        /// </summary>
        public static void LogDecryptionFailure(string filename, string reason)
        {
            LogSecurityEvent("DECRYPT_FAILED", 
                $"File: {SanitizeFilename(filename)}, " +
                $"Reason: {reason}");
        }

        /// <summary>
        /// Logs application start
        /// </summary>
        public static void LogApplicationStart()
        {
            LogSecurityEvent("APP_START", 
                $"CLAVEM started, Version: {GetAppVersion()}");
        }

        /// <summary>
        /// Logs application exit
        /// </summary>
        public static void LogApplicationExit()
        {
            LogSecurityEvent("APP_EXIT", "CLAVEM exited normally");
        }

        private static string GetSecureLogPath()
        {
            if (_logPath != null && File.Exists(_logPath))
                return _logPath;

            try
            {
                // Store logs in %LOCALAPPDATA%\CLAVEM\security.log
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CLAVEM"
                );

                Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, "security.log");

                // Rotate log if too large (>10 MB)
                if (File.Exists(_logPath))
                {
                    var fileInfo = new FileInfo(_logPath);
                    if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        string archivePath = Path.Combine(logDir, 
                            $"security_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.Move(_logPath, archivePath);
                    }
                }

                return _logPath;
            }
            catch
            {
                // Fallback to temp
                return Path.Combine(Path.GetTempPath(), "CLAVEM_security.log");
            }
        }

        private static string SanitizeFilename(string filename)
        {
            // Only log filename, not full path (privacy)
            return Path.GetFileName(filename);
        }

        private static string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
