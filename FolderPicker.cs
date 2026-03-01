// CLAVEM
// Copyright (c) 2026 Massimo Parisi
// SPDX-License-Identifier: AGPL-3.0-only
// Dual-licensed: AGPL-3.0-only or commercial. See LICENSE.
//
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace CLAVEM
{
    internal static class FolderPicker
    {
        /// <summary>
        /// Shows the modern Windows folder picker (IFileOpenDialog with FOS_PICKFOLDERS).
        /// No WinForms dependency required.
        /// Returns null if the user cancels.
        /// </summary>
        public static string? PickFolder(Window owner, string title)
        {
            IntPtr hwndOwner = IntPtr.Zero;
            try
            {
                hwndOwner = new WindowInteropHelper(owner).Handle;
            }
            catch
            {
                // Best-effort: hwndOwner stays zero.
            }

            IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialogRCW();

            try
            {
                dialog.SetTitle(title);

                // Options: pick folders + force filesystem items.
                dialog.GetOptions(out uint options);
                options |= (uint)FOS.FOS_PICKFOLDERS;
                options |= (uint)FOS.FOS_FORCEFILESYSTEM;
                options |= (uint)FOS.FOS_PATHMUSTEXIST;
                dialog.SetOptions(options);

                int hr = dialog.Show(hwndOwner);
                // HRESULT for cancel is 0x800704C7 (ERROR_CANCELLED)
                if (hr == unchecked((int)0x800704C7))
                    return null;

                Marshal.ThrowExceptionForHR(hr);

                dialog.GetResult(out IShellItem item);
                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pszString);

                try
                {
                    if (pszString == IntPtr.Zero)
                        return null;

                    return Marshal.PtrToStringUni(pszString);
                }
                finally
                {
                    if (pszString != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pszString);
                }
            }
            finally
            {
                // Ensure COM object is released.
                if (dialog != null && Marshal.IsComObject(dialog))
                    Marshal.FinalReleaseComObject(dialog);
            }
        }

        // ===== COM interop =====

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW
        { }

        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            // IModalWindow
            [PreserveSig] int Show(IntPtr parent);

            // IFileDialog
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, uint fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);

            // IFileOpenDialog
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_PATHMUSTEXIST = 0x00000800
        }
    }
}
