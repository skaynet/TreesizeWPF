using System;

namespace TreeSizeWPF.Models
{
    public class FileInfoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public long Size { get; set; }
        public string SizeWithUnits { get { return IsProcessScan ? "Анализ..." : AnalyzeDirectoryService.ConvertBytes(Size); } }
        public int Files { get; set; }
        public int Folders { get; set; }
        public DateTime CreationTime { get; set; }
        public string CreationTimeFormatted { get { return CreationTime.ToString("dd.MM.yyyy HH:mm"); } }
        public DateTime LastModified { get; set; }
        public string LastModifiedFormatted { get { return LastModified.ToString("dd.MM.yyyy HH:mm"); } }
        public float PercentUsedFromDisk { get; set; }
        public bool HasChildren { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsProcessScan { get; set; }
    }
}
