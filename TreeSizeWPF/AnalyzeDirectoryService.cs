using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using TreeSizeWPF.Models;

namespace TreeSizeWPF
{
    public class AnalyzeDirectoryService
    {
        private DriveInfo _drive;
        private Dictionary<string, FileInfoItem> _directoryInfoItems;
        private IProgress<long>? _progress;
        private Mutex? _mutexObj;

        public AnalyzeDirectoryService(DriveInfo drive, Dictionary<string, FileInfoItem> directoryInfoItems, IProgress<long>? progress = null, Mutex? mutexObj = null)
        {
            _drive = drive;
            _directoryInfoItems = directoryInfoItems;
            _progress = progress;
            _mutexObj = mutexObj;
        }

        public void AnalyzeDirectoryRoot()
        {
            string directory = _drive.Name;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                _directoryInfoItems[directory] = GetBaseFileInfoDirectory(directoryInfo);

                string[] files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                _directoryInfoItems[directory].Files = files.Length;
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    _directoryInfoItems[directory].Size += fileInfo.Length;
                }
                _progress?.Report(_directoryInfoItems[directory].Size);

                string[] dirs = GetDirectoriesExcludingSymbolicLinks(directory);
                _directoryInfoItems[directory].Folders = dirs.Length;
                if (dirs.Length > 0 || files.Length > 0)
                {
                    _directoryInfoItems[directory].HasChildren = true;
                }
                foreach (string subdirectory in dirs)
                {
                    directoryInfo = new DirectoryInfo(subdirectory);
                    _directoryInfoItems[subdirectory] = GetBaseFileInfoDirectory(directoryInfo);
                    if (GetHasChildrenDirectory(subdirectory))
                    {
                        _directoryInfoItems[subdirectory].HasChildren = true;
                        _directoryInfoItems[subdirectory].IsProcessScan = true;
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _directoryInfoItems[directory].ErrorMessage = ex.Message;
            }
            catch (DirectoryNotFoundException ex)
            {
                _directoryInfoItems[directory].ErrorMessage = ex.Message;
            }
        }

        public (long size, int files, int folders) AnalyzeDirectory(string directory)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                FileInfoItem? fileInfoItemDirectory;
                _mutexObj?.WaitOne();
                fileInfoItemDirectory = GetDirectoryFileInfoItem(directory);
                if (fileInfoItemDirectory == null)
                {
                    _directoryInfoItems[directory] = GetBaseFileInfoDirectory(directoryInfo);
                }
                _mutexObj?.ReleaseMutex();

                (long size, int files, int folders) result = (0, 0, 0);
                string[] files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                result.files = files.Length;
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    result.size += fileInfo.Length;
                }
                _progress?.Report(result.size);

                string[] dirs = GetDirectoriesExcludingSymbolicLinks(directory);
                result.folders = dirs.Length;

                _mutexObj?.WaitOne();
                if (result.files > 0 || result.folders > 0)
                {
                    _directoryInfoItems[directory].HasChildren = true;
                }
                _mutexObj?.ReleaseMutex();

                // Рекурсивный анализ поддиректорий
                foreach (string subdirectory in dirs)
                {
                    (long size, int files, int folders) resultSubdirectory = AnalyzeDirectory(subdirectory);
                    result.size += resultSubdirectory.size;
                    result.files += resultSubdirectory.files;
                    result.folders += resultSubdirectory.folders;
                }
                _mutexObj?.WaitOne();
                _directoryInfoItems[directory].Size = result.size;
                _directoryInfoItems[directory].Files = result.files;
                _directoryInfoItems[directory].Folders = result.folders;
                _directoryInfoItems[directory].PercentUsedFromDisk = (float)Math.Round(result.size / (double)_drive.TotalSize * 100.0, 2);
                _directoryInfoItems[directory].IsProcessScan = false;
                _mutexObj?.ReleaseMutex();
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                _mutexObj?.WaitOne();
                _directoryInfoItems[directory].ErrorMessage = ex.Message;
                _mutexObj?.ReleaseMutex();
                return (0, 0, 0);
            }
            catch (DirectoryNotFoundException ex)
            {
                _mutexObj?.WaitOne();
                _directoryInfoItems[directory].ErrorMessage = ex.Message;
                _mutexObj?.ReleaseMutex();
                return (0, 0, 0);
            }
        }

        public List<FileInfoItem> GetSubdirectoryFileInfoItems(string subdirectory)
        {
            FileInfoItem fileInfoItem = new FileInfoItem();
            List<FileInfoItem> files = new List<FileInfoItem>();
            try
            {
                FileInfoItem? fileInfoItemDirectory;
                foreach (string subsubdirectory in GetDirectoriesExcludingSymbolicLinks(subdirectory))
                {
                    _mutexObj?.WaitOne();
                    fileInfoItemDirectory = GetDirectoryFileInfoItem(subsubdirectory);
                    if (fileInfoItemDirectory == null)
                    {
                        fileInfoItemDirectory = GetBaseFileInfoDirectory(new DirectoryInfo(subsubdirectory));
                        _directoryInfoItems[subsubdirectory] = fileInfoItemDirectory;
                        if (GetHasChildrenDirectory(subsubdirectory))
                        {
                            _directoryInfoItems[subsubdirectory].HasChildren = true;
                            _directoryInfoItems[subsubdirectory].IsProcessScan = true;
                        }
                    }
                    _mutexObj?.ReleaseMutex();
                    files.Add(fileInfoItemDirectory);
                }

                FileInfo fileInfo;
                foreach (string file in Directory.GetFiles(subdirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    fileInfo = new FileInfo(file);
                    fileInfoItem = new FileInfoItem();
                    fileInfoItem.Name = fileInfo.Name;
                    fileInfoItem.Path = fileInfo.FullName;
                    fileInfoItem.Files = 1;
                    fileInfoItem.Size = fileInfo.Length;
                    fileInfoItem.CreationTime = fileInfo.CreationTime;
                    fileInfoItem.LastModified = fileInfo.LastWriteTime;
                    fileInfoItem.PercentUsedFromDisk = (float)Math.Round(fileInfoItem.Size / (double)_drive.TotalSize * 100.0, 2);
                    files.Add(fileInfoItem);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                fileInfoItem.ErrorMessage = ex.Message;
                files.Add(fileInfoItem);
            }
            return files;
        }

        public void CalculateRootDirectoryInfoFromSubdirectory(string subdirectory)
        {
            _mutexObj?.WaitOne();
            FileInfoItem? fileInfoItem = GetDirectoryFileInfoItem(subdirectory);
            if (fileInfoItem != null && _directoryInfoItems[_drive.Name] != null)
            {
                _directoryInfoItems[_drive.Name].Size += fileInfoItem.Size;
                _directoryInfoItems[_drive.Name].Files += fileInfoItem.Files;
                _directoryInfoItems[_drive.Name].Folders += fileInfoItem.Folders;
            }
            _mutexObj?.ReleaseMutex();
        }

        public string[] GetDirectoriesExcludingSymbolicLinks(string directory)
        {
            return Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly).Where(dir => (File.GetAttributes(dir) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint).ToArray();
        }

        public static string ConvertBytes(long bytes, bool isAddUnits = true)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            if (isAddUnits)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", new object[] { len, sizes[order] });
            }
            else
            {
                return len.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }

        private FileInfoItem GetBaseFileInfoDirectory(DirectoryInfo directoryInfo)
        {
            FileInfoItem fileInfoItem = new FileInfoItem();
            fileInfoItem.Name = directoryInfo.Name;
            fileInfoItem.Path = directoryInfo.FullName;
            fileInfoItem.CreationTime = directoryInfo.CreationTime;
            fileInfoItem.LastModified = directoryInfo.LastWriteTime;
            fileInfoItem.IsDirectory = true;
            return fileInfoItem;
        }

        private bool GetHasChildrenDirectory(string directory)
        {
            try
            {
                if (GetDirectoriesExcludingSymbolicLinks(directory).Length > 0
                    || Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private FileInfoItem? GetDirectoryFileInfoItem(string directory)
        {
            FileInfoItem? fileInfoItem;
            if (_directoryInfoItems.TryGetValue(directory, out fileInfoItem))
            {
                return fileInfoItem;
            }
            return null;
        }
    }
}
