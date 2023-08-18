using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TreeSizeWPF.Models;

namespace TreeSizeWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FillListDrivesComboBox();
            progressBar.Minimum = 0;
        }

        private void FillListDrivesComboBox()
        {
            ComboBoxItem comboBoxItem;
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                { continue; }
                comboBoxItem = new ComboBoxItem();
                int percentAvailable = (int)Math.Round(drive.AvailableFreeSpace / (double)drive.TotalSize * 100.0, 0);
                comboBoxItem.Content = $"{drive.Name} {percentAvailable}% Доступно {AnalyzeDirectoryService.ConvertBytes(drive.AvailableFreeSpace, true)}/{AnalyzeDirectoryService.ConvertBytes(drive.TotalSize)} {drive.VolumeLabel}";
                comboBoxItem.Tag = drive;
                ListDrivesComboBox.Items.Add(comboBoxItem);
            }
            ListDrivesComboBox.SelectedIndex = 0;
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeButton.IsEnabled = false;
            _treeList.IsEnabled = false;

            ComboBoxItem? selectedItem = ListDrivesComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag is DriveInfo drive)
            {
                sblFileSystemInfo.Text = drive.DriveFormat;
                progressBar.Maximum = drive.TotalSize - drive.AvailableFreeSpace;
                progressBar.Value = 0;
                Progress<long> progress = new Progress<long>(p => progressBar.Value += p);
                FileInfoModel fileInfoModel = new FileInfoModel(drive, progress, UpdateDataAnalyze, FinishAnalyze);
                fileInfoModel.AnalyzeDirectoryRoot();
                _treeList.Model = fileInfoModel;
                fileInfoModel.AnalyzeAllDirectory();
            }

            _treeList.IsEnabled = true;
        }

        private void FinishAnalyze()
        {
            this.Dispatcher.Invoke(() => {
                AnalyzeButton.IsEnabled = true;
                progressBar.Value = progressBar.Maximum;
            });
        }

        private void UpdateDataAnalyze(FileInfoItem? fileInfoItem)
        {
            this.Dispatcher.Invoke(() => {
                if (fileInfoItem != null)
                {
                    sblFilesCount.Text = "Файлов: " + fileInfoItem.Files.ToString("N0", CultureInfo.InvariantCulture);
                    sblFoldersCount.Text = "Папок: " + fileInfoItem.Folders.ToString("N0", CultureInfo.InvariantCulture);
                }
                _treeList.Items.Refresh();
            });
        }
    }
}
