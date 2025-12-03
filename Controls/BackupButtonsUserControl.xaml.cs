using CoreSQLConnection6;
using static CoreSQLConnection6.My_XM_ReadFileClass;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace BackupFoldersWPF.Controls
{
    /// <summary>
    /// Interaction logic for BackupButtonsUserControl.xaml
    /// </summary>
    public partial class BackupButtonsUserControl : UserControl
    {
        public BackupButtonsUserControl()
        {
            InitializeComponent();
        }

        private void Create7ZipFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindow.Instance.CompressTextBox.Text))
                MessageBox.Show("No Folder Name Dropped");
            else
                MainWindow.Instance.DirectBackupDrop(MainWindow.Instance.CompressTextBox.Text.Replace(",", ""));
        }

        private async void CompressFoldersFiles_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Environment.SpecialFolder.MyDocuments.MY_XM_ExtendSpecialFolder("BackupFolder");

            // Check if the CompressTextBox is empty
            if (string.IsNullOrEmpty(MainWindow.Instance.CompressTextBox.Text))
            {
                // Show a message box if no folder is selected
                MessageBox.Show("Please Select Folder.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow.Instance.CompressTextBox.Focus();
                return; // Exit the method
            }

            string path = MainWindow.Instance.CompressTextBox.Text.Replace(",", "");


            DirectoryInfo parentDirectory = new DirectoryInfo(path);

            // Define directories to be excluded from compression
            string[] excludedDirectories = new string[] { Path.Combine(path, "bin"), Path.Combine(path, "obj") };

            // Check again if the path is not empty
            if (!string.IsNullOrEmpty(path))
            {
                // Reset the progress bar
                MainWindow.Instance.progressBar.Value = 0;

                // Create the ZIP file path with a timestamp
                string ZipFileName = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(path), Path.GetFileNameWithoutExtension(path) + DateTime.Now.ToString("-yyyy-MM-dd HH-mm-ss") + ".zip");


                // Run the compression process in a separate task to avoid blocking the UI thread
                await Task.Run(() =>
                {
                    using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile())
                    {
                        // Iterate through all files in the directory and its subdirectories
                        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        {
                            // Check if the file is not in an excluded directory
                            if (!BackupAndCode.BackupClass.IsExcludedDirectory(file, excludedDirectories))
                            {
                                // Get the relative path of the file
                                string relativePath = Path.GetRelativePath(parentDirectory.Parent.FullName, file).Replace('\\', '/');

                                // Add the file to the ZIP archive with its relative path
                                zip.AddFile(file).FileName = relativePath;

                                // Attach a handler to report the save progress
                                zip.SaveProgress += MainWindow.Instance.Zip_SaveProgress;
                            }
                        }
                        if (!Directory.Exists(Directory.GetParent(ZipFileName).ToString()))
                        {
                            Directory.CreateDirectory(Directory.GetParent(ZipFileName).ToString());
                        }
                        // Save the ZIP archive to the destination path
                        zip.Save(ZipFileName);
                    }
                });
            }
        }

        private void BackupToLocalAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindow.Instance.CompressTextBox.Text))
            {
                MessageBox.Show("Please Select Folder.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow.Instance.CompressTextBox.Focus();
                return;
            }

            string[] FolderNamesArray = MainWindow.Instance.CompressTextBox.Text.Split(',');

            var excludeFileList = new List<Regex>();
            var excludeDirList = new List<Regex>();


            if (MainWindow.Instance.binFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("bin"));
            if (MainWindow.Instance.objFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("obj"));
            if (MainWindow.Instance.tmpFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("tmp"));

            if (!string.IsNullOrEmpty(MainWindow.Instance.FoldersExcludeListTextBox.Text))
            {
                string[] ExcludeListItems = MainWindow.Instance.FoldersExcludeListTextBox.Text.Split(',');
                if (MainWindow.Instance.FoldersRegexMatchCheckBox.IsChecked == true)
                {
                    foreach (string SingleItem in ExcludeListItems)
                    {
                        excludeDirList.Add(new Regex(SingleItem));
                    }
                }

                else
                {
                    foreach (string SingleItem in ExcludeListItems)
                    {
                        excludeDirList.Add(new Regex(Regex.Escape(SingleItem)));
                    }
                }

            }

            if (!string.IsNullOrEmpty(MainWindow.Instance.FilesExcludeTextBox.Text))
            {
                string[] ExcludeListItems = MainWindow.Instance.FilesExcludeTextBox.Text.Split(',');

                if (MainWindow.Instance.FilesRegexMatchCheckBox.IsChecked == true)
                {
                    foreach (string SingleItem in ExcludeListItems)
                    {
                        excludeFileList.Add(new Regex(SingleItem));
                    }
                }
                else
                {
                    foreach (string SingleItem in ExcludeListItems)
                    {
                        excludeFileList.Add(new Regex(Regex.Escape(SingleItem)));
                    }
                }

            }

            string folderPath = Environment.SpecialFolder.MyDocuments.MY_XM_ExtendSpecialFolder("BackupFolder");

            BackupAndCode.BackupClass backupClass = new BackupAndCode.BackupClass();
            backupClass.ZipFolders(folderPath, FolderNamesArray, excludeFileList, excludeDirList);



        }

        private void LoadFolder_Click(object sender, RoutedEventArgs e)
        {
            string FolderName = MY_XM_GetFolderName();

            MainWindow.Instance.CompressTextBox.Text = FolderName;

        }

        private void BackupFormClearFolderName_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.CompressTextBox.Text = "";
        }
    }
}
