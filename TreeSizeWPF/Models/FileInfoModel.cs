using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TreeSizeWPF.TreeList;

namespace TreeSizeWPF.Models
{
    public class FileInfoModel : ITreeModel
    {
        private AnalyzeDirectoryService _analyzeDirectoryService;
        private IProgress<long> _progress;
        private Dictionary<string, FileInfoItem> _directoryInfoItems;
        private DriveInfo _drive;
        private Action<FileInfoItem> _intermediateResultAction;
        private Action _finalResultAction;
        private Mutex _mutexObj;

        public FileInfoModel(DriveInfo drive, IProgress<long> progress, Action<FileInfoItem> intermediateResultAction, Action finalResultAction)
        {
            _directoryInfoItems = new Dictionary<string, FileInfoItem>();
            _mutexObj = new Mutex();
            _analyzeDirectoryService = new AnalyzeDirectoryService(drive, _directoryInfoItems, progress, _mutexObj);
            _progress = progress;
            _drive = drive;
            _intermediateResultAction = intermediateResultAction;
            _finalResultAction = finalResultAction;
        }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
            {
                return _analyzeDirectoryService.GetSubdirectoryFileInfoItems(_drive.Name);
            }
            else
            {
                if (parent is FileInfoItem fileInfoItem)
                {
                    return _analyzeDirectoryService.GetSubdirectoryFileInfoItems(fileInfoItem.Path);
                }
            }
            return new List<FileInfoItem>();
        }

        public bool HasChildren(object parent)
        {
            if (parent is FileInfoItem fileInfoItem)
            {
                return fileInfoItem.HasChildren;
            }
            return false;
        }

        public void AnalyzeDirectoryRoot()
        {
            _analyzeDirectoryService.AnalyzeDirectoryRoot();
        }

        public Task AnalyzeAllDirectory()
        {
            return Task.Run(() => {
                string[] dirs = _analyzeDirectoryService.GetDirectoriesExcludingSymbolicLinks(_drive.Name);
                if (dirs.Length > 0)
                {
                    Task[] tasks = new Task[dirs.Length];
                    int i = 0;
                    foreach (string dir in dirs)
                    {
                        tasks[i++] = AnalyzeDirectoryAsync(dir);
                    }
                    Task.WaitAll(tasks);
                }
                _finalResultAction();
            });
        }

        private async Task AnalyzeDirectoryAsync(string directory)
        {
            await Task.Run(() => {
                AnalyzeDirectoryService analyzeDirectoryService = new AnalyzeDirectoryService(_drive, _directoryInfoItems, _progress, _mutexObj);
                analyzeDirectoryService.AnalyzeDirectory(directory);
                analyzeDirectoryService.CalculateRootDirectoryInfoFromSubdirectory(directory);
                _intermediateResultAction(_directoryInfoItems[_drive.Name]);
            });
        }
    }
}
