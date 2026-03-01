// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CLAVEM.Core
{
    /// <summary>
    /// Helper per gestire crittografia/decrittografia di cartelle
    /// </summary>
    public static class FolderCryptoHelper
    {
        // Limiti di sicurezza (configurabili)
        private const long MaxFolderSizeBytes = 4L * 1024 * 1024 * 1024; // 4 GB
        private const long WarningSizeBytes = 1L * 1024 * 1024 * 1024;   // 1 GB (avviso)
        private const int MaxFileCount = 10000;  // Numero massimo file

        public class FolderAnalysis
        {
            public bool IsValid { get; set; }
            public long TotalSizeBytes { get; set; }
            public int FileCount { get; set; }
            public string? ErrorMessage { get; set; }
            public bool RequiresWarning { get; set; }
            public string? WarningMessage { get; set; }

            public string GetSizeFormatted()
            {
                if (TotalSizeBytes < 1024)
                    return $"{TotalSizeBytes} bytes";
                else if (TotalSizeBytes < 1024 * 1024)
                    return $"{TotalSizeBytes / 1024.0:F2} KB";
                else if (TotalSizeBytes < 1024 * 1024 * 1024)
                    return $"{TotalSizeBytes / (1024.0 * 1024):F2} MB";
                else
                    return $"{TotalSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
            }
        }

        /// <summary>
        /// Analizza una cartella prima della crittografia
        /// </summary>
        public static FolderAnalysis AnalyzeFolder(string folderPath)
        {
            var analysis = new FolderAnalysis { IsValid = false };

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    analysis.ErrorMessage = "Folder does not exist.";
                    return analysis;
                }

                var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                analysis.FileCount = files.Length;

                if (analysis.FileCount == 0)
                {
                    analysis.ErrorMessage = "Folder is empty.";
                    return analysis;
                }

                if (analysis.FileCount > MaxFileCount)
                {
                    analysis.ErrorMessage = $"Folder contains too many files ({analysis.FileCount}). Maximum: {MaxFileCount}";
                    return analysis;
                }

                // Calcola dimensione totale
                analysis.TotalSizeBytes = files.Sum(f => new FileInfo(f).Length);

                if (analysis.TotalSizeBytes > MaxFolderSizeBytes)
                {
                    analysis.ErrorMessage = $"Folder too large ({analysis.GetSizeFormatted()}). Maximum: {MaxFolderSizeBytes / (1024.0 * 1024 * 1024):F1} GB";
                    return analysis;
                }

                // Avviso per cartelle grandi
                if (analysis.TotalSizeBytes > WarningSizeBytes)
                {
                    analysis.RequiresWarning = true;
                    analysis.WarningMessage = $"Large folder ({analysis.GetSizeFormatted()}, {analysis.FileCount} files). This may take several minutes and require significant RAM. Continue?";
                }

                analysis.IsValid = true;
                return analysis;
            }
            catch (UnauthorizedAccessException)
            {
                analysis.ErrorMessage = "Access denied. Check folder permissions.";
                return analysis;
            }
            catch (Exception ex)
            {
                analysis.ErrorMessage = $"Error analyzing folder: {ex.Message}";
                return analysis;
            }
        }

        /// <summary>
        /// Crea un archivio ZIP temporaneo della cartella
        /// </summary>
        public static string CreateTemporaryZip(string folderPath, string? tempPath = null)
        {
            if (string.IsNullOrEmpty(tempPath))
            {
                tempPath = Path.Combine(Path.GetTempPath(), $"CLAVEM_{Guid.NewGuid()}.zip");
            }

            try
            {
                // Crea ZIP con compressione ottimale
                ZipFile.CreateFromDirectory(folderPath, tempPath, CompressionLevel.Optimal, false);
                return tempPath;
            }
            catch (Exception ex)
            {
                // Pulisci il file temporaneo se fallisce
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw new Exception($"Failed to create ZIP archive: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Estrae uno ZIP in una cartella
        /// </summary>
        
public static void ExtractZipToFolder(string zipPath, string outputFolder)
{
    try
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        SafeExtractZipToFolder(zipPath, outputFolder);
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to extract archive: {ex.Message}", ex);
    }
}

/// <summary>
/// Safe ZIP extraction (prevents Zip Slip and avoids overwriting existing files)
/// </summary>
private static void SafeExtractZipToFolder(string zipPath, string outputFolder)
{
    string baseFull = Path.GetFullPath(outputFolder);
    if (!baseFull.EndsWith(Path.DirectorySeparatorChar))
        baseFull += Path.DirectorySeparatorChar;

    using (var archive = ZipFile.OpenRead(zipPath))
    {
        foreach (var entry in archive.Entries)
        {
            // Normalize to a destination path
            string destinationPath = Path.GetFullPath(Path.Combine(outputFolder, entry.FullName));

            // Zip Slip protection
            if (!destinationPath.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Archive contains invalid paths.");

            // Directories
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            // No overwrite: pick a unique path
            destinationPath = GetUniquePath(destinationPath);

            entry.ExtractToFile(destinationPath, overwrite: false);
        }
    }
}

private static string GetUniquePath(string path)
{
    if (!File.Exists(path) && !Directory.Exists(path))
        return path;

    string directory = Path.GetDirectoryName(path) ?? "";
    string filename = Path.GetFileNameWithoutExtension(path);
    string extension = Path.GetExtension(path);

    if (string.IsNullOrWhiteSpace(filename))
        filename = "file";

    for (int i = 1; i < 10000; i++)
    {
        string candidate = Path.Combine(directory, $"{filename} ({i}){extension}");
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
            return candidate;
    }

    throw new IOException("Unable to create a unique extracted filename.");
}

/// <summary>
/// Pulisce file temporanei in modo sicuro
/// </summary>
        /// </summary>
        public static void CleanupTempFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    // Usa secure delete per i file temporanei
                    CryptoEngine.SecureDelete(filePath, passes: 1); // 1 pass è sufficiente per temp
                }
                catch
                {
                    // Fallback: cancellazione normale
                    try { File.Delete(filePath); } catch { }
                }
            }
        }
    }
}
