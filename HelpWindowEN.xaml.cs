// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
using System.Windows;

namespace CLAVEM
{
    public partial class HelpWindowEN : Window
    {
        public HelpWindowEN()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
