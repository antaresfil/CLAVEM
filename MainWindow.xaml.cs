// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CLAVEM.Core;

namespace CLAVEM
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            UpdateOperationModeUI();
            ValidateInputs();
        }

        private void OperationMode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateOperationModeUI();
            UpdateFileInfo();
            ValidateInputs();
        }

        private void UpdateOperationModeUI()
        {
            bool isEncrypting = EncryptRadioButton?.IsChecked == true;

            if (ConfirmPasswordPanel != null)
                ConfirmPasswordPanel.Visibility = isEncrypting ? Visibility.Visible : Visibility.Collapsed;

            // Aggiorna testo bottone Execute
            if (ExecuteButton != null)
                ExecuteButton.Content = isEncrypting ? "Encrypt File" : "Decrypt File";

            // When switching to decrypt, clear confirmation UI to avoid confusion
            if (!isEncrypting)
            {
                if (ConfirmPasswordBox != null) ConfirmPasswordBox.Password = string.Empty;
                if (PasswordMismatchText != null) PasswordMismatchText.Visibility = Visibility.Collapsed;
            }

            // Ricalcola FileInfoPanel e warning in base alla nuova modalità
            UpdateFileInfo();
        }

        

private static byte[] SecureStringToUtf8Bytes(SecureString secureString)
{
    if (secureString == null)
        throw new ArgumentNullException(nameof(secureString));

    IntPtr bstr = IntPtr.Zero;
    char[]? chars = null;

    try
    {
        bstr = Marshal.SecureStringToBSTR(secureString);

        // BSTR length is stored in bytes at (ptr - 4)
        int byteLen = Marshal.ReadInt32(bstr, -4);
        int charLen = byteLen / 2;

        chars = new char[charLen];
        Marshal.Copy(bstr, chars, 0, charLen);

        // NOTE: this creates a byte[] on the managed heap; we zero it after use.
        return Encoding.UTF8.GetBytes(chars);
    }
    finally
    {
        if (chars != null)
            Array.Clear(chars, 0, chars.Length);

        if (bstr != IntPtr.Zero)
            Marshal.ZeroFreeBSTR(bstr);
    }
}

private static bool FixedTimeEquals(byte[] a, byte[] b)
{
    if (a == null || b == null) return false;
    if (a.Length != b.Length) return false;
    return CryptographicOperations.FixedTimeEquals(a, b);
}

private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            bool isEncrypting = EncryptRadioButton?.IsChecked == true;

            if (!isEncrypting)
            {
                // ===== DECRYPT: apre direttamente il file picker per file .svlt =====
                var ofd = new OpenFileDialog
                {
                    Title = "Select an encrypted file to decrypt",
                    CheckFileExists = true,
                    Filter = "Encrypted files (*.svlt)|*.svlt|All files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    FilePathTextBox.Text = ofd.FileName;
                    UpdateFileInfo();
                }
                return;
            }

            // ===== ENCRYPT: chiede se file o cartella =====
            var choice = MessageBox.Show(
                "What do you want to encrypt?\n\n" +
                "Click YES to select a FILE\n" +
                "Click NO to select a FOLDER\n" +
                "Click CANCEL to abort",
                "Select File or Folder",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel)
                return;

            if (choice == MessageBoxResult.Yes)
            {
                // ===== SELEZIONA FILE =====
                var ofd = new OpenFileDialog
                {
                    Title = "Select a file to encrypt",
                    CheckFileExists = true,
                    Filter = "All files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    FilePathTextBox.Text = ofd.FileName;
                    UpdateFileInfo();
                }
            }
            else
            {
                // ===== SELEZIONA CARTELLA =====
                var folder = FolderPicker.PickFolder(this, "Select a folder to encrypt (will be saved as .zip.svlt)");
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    FilePathTextBox.Text = folder;
                    UpdateFileInfo();
                }
            }
        }

        private void FilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFileInfo();
        }

        private void UpdateFileInfo()
        {
            if (FileInfoPanel == null || FileInfoText == null || FileInfoDetails == null)
                return;

            string filePath = FilePathTextBox.Text;
            bool isEncrypting = EncryptRadioButton?.IsChecked == true;

            if (string.IsNullOrEmpty(filePath))
            {
                FileInfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // In modalità Encrypt: blocca se il file è già un .svlt
            if (isEncrypting && filePath.EndsWith(".svlt", StringComparison.OrdinalIgnoreCase))
            {
                FileInfoText.Text = string.Empty;
                FileInfoDetails.Text = string.Empty;
                if (EncryptedFileWarning != null) EncryptedFileWarning.Visibility = Visibility.Visible;
                FileInfoPanel.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF0F0"));
                FileInfoPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0A0A0"));
                FileInfoPanel.Visibility = Visibility.Visible;
                return;
            }

            // Nascondi se stiamo cifrando un file normale o se il file non esiste
            if (!File.Exists(filePath) || isEncrypting)
            {
                FileInfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // In decrypt: mostra info solo per file .svlt
            if (!filePath.EndsWith(".svlt", StringComparison.OrdinalIgnoreCase))
            {
                FileInfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Reset colori pannello (stile normale per decrypt)
            FileInfoPanel.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF8EC"));
            FileInfoPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0D4B0"));
            if (EncryptedFileWarning != null) EncryptedFileWarning.Visibility = Visibility.Collapsed;

            var metadata = CryptoEngine.ReadFileMetadata(filePath);

            if (metadata != null && metadata.IsValid)
            {
                FileInfoPanel.Visibility = Visibility.Visible;
                FileInfoText.Text = metadata.GetAuthenticationInfo();
                string details = $"File version: {metadata.Version} | Encrypted file. Enter your password and, if you used one, select the correct keyfile.";
                FileInfoDetails.Text = details;
            }
            else
            {
                FileInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BrowseKeyFileButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select keyfile",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == true)
            {
// Safety: keyfile must not be the same as the input file
try
{
    var input = FilePathTextBox?.Text?.Trim();
    var key = ofd.FileName?.Trim();

    if (!string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(key))
    {
        var fullInput = Path.GetFullPath(input);
        var fullKey = Path.GetFullPath(key);

        if (string.Equals(fullInput, fullKey, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "The keyfile cannot be the same as the file being processed.\n\nSelect a different keyfile.",
                "Unsafe selection",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }
    }
}
catch
{
    // If path normalization fails, let ValidateInputs()/ExecuteAsync handle it
}

                KeyFilePathTextBox.Text = ofd.FileName;
                UseKeyFileCheckBox.IsChecked = true;
            }
        }

        private void SetupGuideButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new SetupGuideWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void VerifyIntegrityButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "CLAVEM — Verify Program Integrity\n\n" +
                "To verify the integrity of this application,\n" +
                "compare the SHA-256 hash of the executable\n" +
                "with the official hash published on GitHub:\n\n" +
                "github.com/antaresfil/CLAVEM\n\n" +
                "Support: clavemhelp@noxfarm.com",
                "Verify Integrity",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void HelpIT_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindowIT();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void HelpEN_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindowEN();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void AuthFactor_Changed(object sender, RoutedEventArgs e)
        {
            if (UseKeyFileCheckBox?.IsChecked != true)
            {
                // UX: when disabling keyfile, clear the path to avoid confusion and stale state
                if (KeyFilePathTextBox != null)
                    KeyFilePathTextBox.Text = string.Empty;
            }

            ValidateInputs();
        }

        private void AuthFactor_Changed(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ExecuteAsync();
        }
        private async Task ExecuteAsync()
        {
            bool errorAlreadyShown = false;

            try
            {
                string inputPath = FilePathTextBox.Text;

                // Determina se è un file o una cartella
                bool isFolder = Directory.Exists(inputPath);
                bool isFile = File.Exists(inputPath);

                if (!isFolder && !isFile)
                {
                    MessageBox.Show("Selected path does not exist.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool isEncrypting = EncryptRadioButton.IsChecked == true;

                // SECURITY: derive the master key from SecureString without keeping plaintext
                // password strings alive longer than necessary. We extract UTF-8 bytes from
                // SecureString into managed byte[] (best-effort), then zero them immediately
                // after use.
                byte[]? passwordBytes = null;
                byte[]? confirmBytes = null;

                try
                {
                    passwordBytes = SecureStringToUtf8Bytes(PasswordBox.SecurePassword);

                    if (passwordBytes == null || passwordBytes.Length == 0)
                    {
                        MessageBox.Show("Password is required.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Encrypt safety: require password confirmation match (fixed-time compare)
                    if (isEncrypting)
                    {
                        if (ConfirmPasswordBox?.SecurePassword == null || ConfirmPasswordBox.SecurePassword.Length == 0)
                        {
                            MessageBox.Show("Please confirm the password before encrypting.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        confirmBytes = SecureStringToUtf8Bytes(ConfirmPasswordBox.SecurePassword);

                        if (!FixedTimeEquals(passwordBytes, confirmBytes))
                        {
                            MessageBox.Show("Passwords do not match. Please re-enter and confirm the password before encrypting.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

// Safety: prevent using the same file as both input and keyfile
if (UseKeyFileCheckBox.IsChecked == true)
{
    var keyPath = KeyFilePathTextBox.Text?.Trim();

    if (!string.IsNullOrWhiteSpace(keyPath))
    {
        string fullInput = Path.GetFullPath(inputPath.Trim());
        string fullKey = Path.GetFullPath(keyPath);

        if (string.Equals(fullInput, fullKey, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Unsafe configuration: the keyfile cannot be the same as the file being processed.\n\nSelect a different keyfile or disable keyfile mode.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
    }
}

                    // Se è una cartella e stiamo crittografando, analizza prima
                    if (isFolder && isEncrypting)
                    {
                        var analysis = FolderCryptoHelper.AnalyzeFolder(inputPath);

                        if (!analysis.IsValid)
                        {
                            MessageBox.Show($"Cannot encrypt folder:\n\n{analysis.ErrorMessage}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // Mostra warning se cartella grande
                        if (analysis.RequiresWarning)
                        {
                            var result = MessageBox.Show(
                                $"{analysis.WarningMessage}\n\n" +
                                $"Size: {analysis.GetSizeFormatted()}\n" +
                                $"Files: {analysis.FileCount}\n\n" +
                                "This may take several minutes. Continue?",
                                "Large Folder Warning",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);

                            if (result != MessageBoxResult.Yes)
                                return;
                        }
                    }

                    ProgressPanel.Visibility = Visibility.Visible;
                    ExecuteButton.IsEnabled = false;
                    ProgressText.Text = isEncrypting ? "Encrypting..." : "Decrypting...";

                    try
                    {
                        await Task.Run(() =>
                        {
                            byte[]? keyFileBytes = null;
                            bool useKeyfile = false;

                            Dispatcher.Invoke(() =>
                            {
                                useKeyfile = UseKeyFileCheckBox.IsChecked == true;

                                if (useKeyfile)
                                {
                                    if (string.IsNullOrWhiteSpace(KeyFilePathTextBox.Text) || !File.Exists(KeyFilePathTextBox.Text))
                                        throw new UnauthorizedAccessException("Keyfile is required but was not selected or does not exist.");

                                    keyFileBytes = AuthenticationManager.ReadKeyFileBytes(KeyFilePathTextBox.Text);
                                }
                            });

                            byte[] masterKey = AuthenticationManager.DeriveMasterKey(passwordBytes!, keyFileBytes);

                            // SECURITY: zero keyFileBytes immediately after key derivation.
                            // DeriveMasterKey has already consumed and internally cloned the material.
                            // Keeping keyFileBytes alive until the end of the operation is unnecessary
                            // and increases the window during which keyfile content sits in managed memory.
                            if (keyFileBytes != null)
                            {
                                CryptographicOperations.ZeroMemory(keyFileBytes);
                                keyFileBytes = null;
                            }

                            bool secureDelete = false;

                            Dispatcher.Invoke(() =>
                            {
                                secureDelete = SecureDeleteCheckBox.IsChecked == true;
                            });

                            string? tempZipPath = null;

                            try
                            {
                                if (isEncrypting)
                                {
                                    string fileToEncrypt = inputPath;

                                    // Se è una cartella, crea ZIP temporaneo
                                    if (isFolder)
                                    {
                                        Dispatcher.Invoke(() => ProgressText.Text = "Creating ZIP archive...");
                                        tempZipPath = FolderCryptoHelper.CreateTemporaryZip(inputPath);
                                        fileToEncrypt = tempZipPath;
                                    }

                                    Dispatcher.Invoke(() => ProgressText.Text = "Encrypting...");
                                    string outputPath = inputPath + (isFolder ? ".zip.svlt" : ".svlt");
                                    CryptoEngine.EncryptFile(fileToEncrypt, outputPath, masterKey, useKeyfile);

                                    // Secure delete dell'originale (file o cartella)
                                    if (secureDelete)
                                    {
                                        if (isFolder)
                                        {
                                            Dispatcher.Invoke(() => ProgressText.Text = "Securely deleting folder...");
                                            // SECURITY FIX: Directory.Delete() does NOT overwrite file contents.
                                            // We must securely wipe every file before removing the directory tree,
                                            // otherwise the plaintext remains recoverable with forensic tools.
                                            foreach (string fileToWipe in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                                            {
                                                CryptoEngine.SecureDelete(fileToWipe);
                                            }
                                            Directory.Delete(inputPath, true); // Now safe: contents already wiped
                                        }
                                        else
                                        {
                                            CryptoEngine.SecureDelete(inputPath);
                                        }
                                    }

                                    // Pulisci ZIP temporaneo
                                    if (tempZipPath != null)
                                    {
                                        FolderCryptoHelper.CleanupTempFile(tempZipPath);
                                    }
                                }
                                else
                                {
                                    // Decrittografia
                                    string outputPath;

                                    if (inputPath.EndsWith(".zip.svlt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Era una cartella
                                        outputPath = inputPath.Substring(0, inputPath.Length - 9) + "_decrypted.zip";
                                    }
                                    else if (inputPath.EndsWith(".svlt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Era un file
                                        outputPath = inputPath.Substring(0, inputPath.Length - 5);
                                    }
                                    else
                                    {
                                        outputPath = inputPath + ".decrypted";
                                    }

                                    CryptoEngine.DecryptFile(inputPath, outputPath, masterKey, keyFileBytes);
                                }
                            }
                            finally
                            {
                                CryptographicOperations.ZeroMemory(masterKey);
                                if (keyFileBytes != null) CryptographicOperations.ZeroMemory(keyFileBytes);

                                // Cleanup finale ZIP temporaneo (se esiste ancora)
                                if (tempZipPath != null)
                                {
                                    FolderCryptoHelper.CleanupTempFile(tempZipPath);
                                }
                            }
                        });

                        if (isEncrypting)
                            OutputPathTextBox.Text = inputPath + (isFolder ? ".zip.svlt" : ".svlt");
                        else
                        {
                            if (inputPath.EndsWith(".zip.svlt", StringComparison.OrdinalIgnoreCase))
                                OutputPathTextBox.Text = inputPath.Substring(0, inputPath.Length - 9) + "_decrypted.zip";
                            else if (inputPath.EndsWith(".svlt", StringComparison.OrdinalIgnoreCase))
                                OutputPathTextBox.Text = inputPath.Substring(0, inputPath.Length - 5);
                            else
                                OutputPathTextBox.Text = inputPath + ".decrypted";
                        }

                        MessageBox.Show(isEncrypting ? "Encryption successful!" : "Decryption successful!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Reset del form dopo successo
                        ResetForm();
                    }
                    catch (Exception ex)
                    {
                        errorAlreadyShown = true;
                        MessageBox.Show(
                            $"Operation failed!\n\n{ex.GetType().Name}: {ex.Message}",
                            "Operation failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }
                finally
                {
                    if (passwordBytes != null) CryptographicOperations.ZeroMemory(passwordBytes);
                    if (confirmBytes != null) CryptographicOperations.ZeroMemory(confirmBytes);
                }
            }
            catch (Exception ex)
            {
                // Catch di sicurezza - cattura TUTTO
                if (!errorAlreadyShown)
                {
                    MessageBox.Show($"Unexpected error!\n\n{ex.GetType().Name}: {ex.Message}",
                        "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ExecuteButton.IsEnabled = true;
                ValidateInputs();
            }
        }

        private void ValidateInputs()
        {
            if (FilePathTextBox == null || PasswordBox == null || UseKeyFileCheckBox == null ||
                KeyFilePathTextBox == null || ExecuteButton == null)
                return;

            // Accetta sia file che cartelle
            string path = FilePathTextBox.Text;
            bool hasValidPath = !string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path));
            bool hasPassword = PasswordBox.SecurePassword != null && PasswordBox.SecurePassword.Length > 0;
            bool isEncrypting = EncryptRadioButton?.IsChecked == true;

            // Blocca se si tenta di cifrare un file già cifrato
            bool isAlreadyEncrypted = isEncrypting &&
                path.EndsWith(".svlt", StringComparison.OrdinalIgnoreCase);

            if (EncryptedFileWarning != null)
                EncryptedFileWarning.Visibility = isAlreadyEncrypted ? Visibility.Visible : Visibility.Collapsed;

            
// Confirm password is mandatory only when encrypting
bool hasConfirm = true;
bool passwordsMatch = true;

if (isEncrypting)
{
    hasConfirm = ConfirmPasswordBox?.SecurePassword != null && ConfirmPasswordBox.SecurePassword.Length > 0;

    if (hasPassword && hasConfirm)
{
    byte[]? pwdBytes = null;
    byte[]? confirmBytes = null;

    try
    {
        pwdBytes = SecureStringToUtf8Bytes(PasswordBox.SecurePassword);

        var confirmSecure = ConfirmPasswordBox?.SecurePassword;
        if (confirmSecure == null)
        {
            // Rare WPF initialization edge-case: treat as not confirmed / mismatch
            passwordsMatch = false;
        }
        else
        {
            confirmBytes = SecureStringToUtf8Bytes(confirmSecure);
            passwordsMatch = FixedTimeEquals(pwdBytes, confirmBytes);
        }
    }
    finally
    {
        if (pwdBytes != null) CryptographicOperations.ZeroMemory(pwdBytes);
        if (confirmBytes != null) CryptographicOperations.ZeroMemory(confirmBytes);
    }
}
else
    {
        passwordsMatch = false;
    }

    // UI feedback (only when user typed something)
    if (PasswordMismatchText != null)
    {
        bool showMismatch = hasPassword && hasConfirm && !passwordsMatch;
        PasswordMismatchText.Visibility = showMismatch ? Visibility.Visible : Visibility.Collapsed;
    }
}
else
{
    if (PasswordMismatchText != null)
        PasswordMismatchText.Visibility = Visibility.Collapsed;
}

bool keyfileOk = true;
            if (UseKeyFileCheckBox.IsChecked == true)
            {
                keyfileOk = !string.IsNullOrEmpty(KeyFilePathTextBox.Text) && File.Exists(KeyFilePathTextBox.Text);
            }

            ExecuteButton.IsEnabled = hasValidPath && hasPassword && keyfileOk && !isAlreadyEncrypted && (!isEncrypting || (hasConfirm && passwordsMatch));
        }

        private void ResetForm()
        {
            // Pulisce tutti i campi dopo un'operazione completata
            FilePathTextBox.Text = string.Empty;
            OutputPathTextBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;
            if (ConfirmPasswordBox != null) ConfirmPasswordBox.Password = string.Empty;
            KeyFilePathTextBox.Text = string.Empty;
            
            // Reset dei checkbox ai valori di default
            UseKeyFileCheckBox.IsChecked = false;
            SecureDeleteCheckBox.IsChecked = true;
            
            // Reset dell'operazione a Encrypt di default
            EncryptRadioButton.IsChecked = true;
            
            // Nascondi file info panel
            if (FileInfoPanel != null)
                FileInfoPanel.Visibility = Visibility.Collapsed;
            
            // Aggiorna lo stato dei pulsanti
            UpdateOperationModeUI();
            ValidateInputs();
        }

private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            AuthFactor_Changed(sender, e);
        }

    }
}