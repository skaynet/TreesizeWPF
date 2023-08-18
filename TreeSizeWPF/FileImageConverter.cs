using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using TreeSizeWPF.Models;
using System.Drawing;

namespace TreeSizeWPF
{
    internal class FileImageConverter : IValueConverter
    {
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const int SIZE_ICON = 16;

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public object Convert(object obj, Type type, object parameter, CultureInfo culture)
        {
            if (obj is FileInfoItem fileInfoItem)
            {
                Icon? icon;
                if (fileInfoItem.IsDirectory)
                {
                    icon = GetFolderIcon(fileInfoItem.Path);
                    if (icon == null)
                    {
                        return "/Images/folder.png";
                    }
                }
                else
                {
                    icon = GetFileIcon(fileInfoItem.Path);
                    if (icon == null)
                    {
                        return "/Images/data.png";
                    }
                }
                return ConvertIconToBitmapSource(icon);
            }
            else
                return "/Images/folder.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Icon? GetFolderIcon(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;
                IntPtr hImg = SHGetFileInfo(folderPath, FILE_ATTRIBUTE_DIRECTORY, out shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                if (hImg != IntPtr.Zero)
                {
                    return Icon.FromHandle(shinfo.hIcon);
                }
            }
            return null;
        }

        private static Icon? GetFileIcon(string filePath)
        {
            if (File.Exists(filePath))
            {
                return Icon.ExtractAssociatedIcon(filePath);
            }
            return null;
        }

        private static BitmapSource ConvertIconToBitmapSource(Icon icon)
        {
            if (icon.Width == SIZE_ICON && icon.Height == SIZE_ICON)
            {
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            else
            {
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(16, 16));
            }
        }
    }
}
