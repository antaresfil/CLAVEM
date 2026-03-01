// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CLAVEM
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            VersionText.Text = $"Version {GetAppVersion()}";
        }

        private static string GetAppVersion()
        {
            // Prefer a human-friendly version string (e.g., SemVer) set via <InformationalVersion>.
            var asm = Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
                return info;

            // Fallback to the assembly version.
            return asm.GetName().Version?.ToString() ?? "0.0.0";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Apre il client email predefinito
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
