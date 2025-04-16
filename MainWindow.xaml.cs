using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using CoreSQLConnection6;
using static CoreSQLConnection6.My_XM_ReadFileClass;
using System.Windows.Threading;
using System.Diagnostics;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Archives.Tar;
using SharpCompress.Writers;
using SevenZip;



namespace BackupFoldersWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int SecondsCounter = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        string FolderPath = "";
        public MainWindow(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                FolderPath = args[0];
                GetExplorerFolderName();
            }

        }
        ArrayList selectedFolder = new ArrayList();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetLongPathName(string path, StringBuilder longPath, int longPathLength);
        private void GetExplorerFolderName()
        {
            StringBuilder longPath = new StringBuilder(255);
            GetLongPathName(FolderPath, longPath, longPath.Capacity);
            CompressTextBox.Text = longPath.ToString() + ",";
        }


        private void CreateDummyFilesMethod()
        {
            string SelectedPath = MY_XM_GetFolderName();

            /*
             * 
             * IF/ELSE REMOVED IN FAVOUR OF CONDITIONAL OPERATOR
             * if (string.IsNullOrEmpty(FoldersExcludeListTextBox.Text))
             *     FoldersExcludeListTextBox.AppendText(Path.GetFileName(TempUpperFolderDialog.SelectedPath));
             * else
             *     FoldersExcludeListTextBox.AppendText("," + Path.GetFileName(TempUpperFolderDialog.SelectedPath));
             * 
             */

            FoldersExcludeListTextBox.AppendText(string.IsNullOrEmpty(FoldersExcludeListTextBox.Text) ? Path.GetFileName(SelectedPath) : "," + Path.GetFileName(SelectedPath));

        }



        private void LoadFolder_Click(object sender, RoutedEventArgs e)
        {
            string FolderName = MY_XM_GetFolderName();

            CompressTextBox.Text = FolderName;


        }


        private async void CompressFoldersFiles_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Environment.SpecialFolder.MyDocuments.MY_XM_ExtendSpecialFolder("BackupFolder");

            // Check if the CompressTextBox is empty
            if (string.IsNullOrEmpty(CompressTextBox.Text))
            {
                // Show a message box if no folder is selected
                MessageBox.Show("Please Select Folder.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                CompressTextBox.Focus();
                return; // Exit the method
            }

            string path = CompressTextBox.Text.Replace(",", "");


            DirectoryInfo parentDirectory = new DirectoryInfo(path);

            // Define directories to be excluded from compression
            string[] excludedDirectories = new string[] { Path.Combine(path, "bin"), Path.Combine(path, "obj") };

            // Check again if the path is not empty
            if (!string.IsNullOrEmpty(path))
            {
                // Reset the progress bar
                progressBar.Value = 0;

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
                                zip.SaveProgress += Zip_SaveProgress;
                            }
                        }

                        // Save the ZIP archive to the destination path
                        zip.Save(ZipFileName);
                    }
                });
            }
        }

        private void Zip_SaveProgress(object sender, Ionic.Zip.SaveProgressEventArgs e)
        {

            if (e.EventType == Ionic.Zip.ZipProgressEventType.Saving_BeforeWriteEntry)
            {



                double progressPercentage = ((double)e.EntriesSaved / e.EntriesTotal) * 100;

                // Access the Dispatcher from the non-UI thread
                Dispatcher dispatcher = progressBar.Dispatcher;

                if (dispatcher.CheckAccess())
                {
                    // Already on the UI thread, update directly
                    progressBar.Value = progressPercentage;
                }
                else
                {

                    // Not on the UI thread, invoke on the UI thread
                    dispatcher.Invoke(() =>
                    {
                        SavedEntriesTextBox.Text = (e.EntriesSaved + 1).ToString() + "/" + e.EntriesTotal.ToString();
                        progressBar.Value = progressPercentage;
                    });
                }

            }
        }


        private void Create7ZipFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CompressTextBox.Text))
                MessageBox.Show("No Folder Name Dropped");
            else
                DirectBackupDrop(CompressTextBox.Text.Replace(",", ""));
        }

        private void DirectBackupDrop(string DropedPath)
        {

            string sevenZipPath = @"C:\Program Files\7-Zip\7z.exe";


            DirectoryInfo ParentDirectoryBackup = new DirectoryInfo(DropedPath);

            string destinationFile = Path.Combine(ParentDirectoryBackup.Parent.FullName, Path.GetFileNameWithoutExtension(DropedPath) + DateTime.Now.ToString("-yyyy-MM-dd HH-mm-ss") + ".7z");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"a -t7z \"{destinationFile}\" \"{DropedPath}\" -mx0 -xr!bin -xr!obj",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            try
            {

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

                MessageBox.Show("Compression completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during compression: " + ex.Message);
            }
        }

        private void CompressTextBox_PreviewEnter(object sender, DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.All;
        }

        private void CompressTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;  // Allow copying files
                VisualStateManager.GoToState(this, "DragOver", true); // Apply a visual state for drag-over (optional)
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;  // Don't allow drop if not a file
            }
        }

        private void CompressTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] Dropedfiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (DirectDropBackupCheckBox.IsChecked == true)
            {
                if (Directory.Exists(Dropedfiles[0]))
                    CreateTarGzArchive(Dropedfiles[0]);
                else if (System.IO.File.Exists(Dropedfiles[0]))
                    MessageBox.Show("Please Drop a Folder!");
                else
                    return;
            }
            if (ClearCompressCheckBox.IsChecked == true) CompressTextBox.Text = "";
            foreach (var file in Dropedfiles)
            {
                CompressTextBox.Text += file + ",";
            }
        }

        public void CreateTarGzArchive(string sourceDirectory)
        {
            DirectoryInfo parentDirectory = new DirectoryInfo(sourceDirectory);

            string folderPath = Environment.SpecialFolder.MyDocuments.MY_XM_ExtendSpecialFolder("BackupFolder");

            string FileType = "";

            if (TarGzRadioButton.IsChecked == true)
            {
                FileType = ".tar.gz";
            }
            else if (SevenZipRadioButton.IsChecked == true)
            {
                FileType = ".7z";
            }
            else if (ZipRadioButton.IsChecked == true)
            {
                FileType = ".zip";
            }

            string destinationFile = Path.Combine(folderPath, Path.GetFileName(sourceDirectory),
                                                   Path.GetFileNameWithoutExtension(sourceDirectory) +
                                                   DateTime.Now.ToString("-yyyy-MM-dd HH-mm-ss") + FileType);

            string[] excludedDirectories = new string[] { Path.Combine(sourceDirectory, "bin"), Path.Combine(sourceDirectory, "obj") };

            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' not found.");
            }

            if (TarGzRadioButton.IsChecked == true)
            {
                SelectTarGzType(sourceDirectory, excludedDirectories, parentDirectory.Parent.FullName, destinationFile);
            }
            else if (SevenZipRadioButton.IsChecked == true)
            {
                SelectSevenZipType(sourceDirectory, excludedDirectories, destinationFile);
            }
            else if (ZipRadioButton.IsChecked == true)
            {
                SelectZipType(sourceDirectory, destinationFile, parentDirectory.Parent.FullName);
            }

        }

        private void SelectTarGzType(string sourceDirectory, string[] excludedDirectories, string ParentDirectory, string destinationFile)
        {
            using (var archive = TarArchive.Create())
            {
                foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    if (!BackupAndCode.BackupClass.IsExcludedDirectory(file, excludedDirectories))
                    {
                        archive.AddEntry(Path.GetRelativePath(ParentDirectory, file), file);
                    }
                }

                using (var stream = File.Create(destinationFile))
                {
                    archive.SaveTo(stream, CompressionType.GZip);
                }
            }
        }

        public static void SelectSevenZipType(string sourceDirectory, string[] excludedDirectories, string destinationFile)
        {
            // Assuming 7-Zip is installed and accessible in your system path
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\Program Files\7-Zip\7z.exe";

            // Build the 7-Zip command-line arguments
            string arguments = $"a -t7z -r \"{destinationFile}\" \"{sourceDirectory}\" -mx0 -xr!bin -xr!obj";


            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

            }
        }

        static void SelectZipType(string sourceDirectory, string zipFilePath, string ParentDirectory)
        {

            using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
            {
                AddDirectoryToArchive(archive, sourceDirectory, ParentDirectory);
                using (var zipStream = File.OpenWrite(zipFilePath))
                {
                    archive.SaveTo(zipStream, new WriterOptions(CompressionType.Deflate)
                    {
                        ArchiveEncoding = new ArchiveEncoding(System.Text.Encoding.UTF8, System.Text.Encoding.UTF8)
                    });
                }
            }

        }

        static void AddDirectoryToArchive(SharpCompress.Archives.Zip.ZipArchive archive, string sourceDirectory, string baseDirectory)
        {
            foreach (string directoryPath in Directory.GetDirectories(sourceDirectory))
            {
                // Skip bin and obj directories
                if (directoryPath.EndsWith("bin") || directoryPath.EndsWith("obj"))
                {
                    continue;
                }
                // Add subdirectories recursively
                AddDirectoryToArchive(archive, directoryPath, baseDirectory);
            }

            foreach (string filePath in Directory.GetFiles(sourceDirectory))
            {
                // Create a relative path for each file
                string relativePath = Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');
                archive.AddEntry(relativePath, filePath);
            }
        }


        private void BackupFormClearFolderName_Click(object sender, RoutedEventArgs e)
        {
            CompressTextBox.Text = "";
        }


        private void BackupToLocalAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CompressTextBox.Text))
            {
                MessageBox.Show("Please Select Folder.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                CompressTextBox.Focus();
                return;
            }

            string[] FolderNamesArray = CompressTextBox.Text.Split(',');

            var excludeFileList = new List<Regex>();
            var excludeDirList = new List<Regex>();


            if (binFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("bin"));
            if (objFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("obj"));
            if (tmpFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("tmp"));

            if (!string.IsNullOrEmpty(FoldersExcludeListTextBox.Text))
            {
                string[] ExcludeListItems = FoldersExcludeListTextBox.Text.Split(',');
                if (FoldersRegexMatchCheckBox.IsChecked == true)
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

            if (!string.IsNullOrEmpty(FilesExcludeTextBox.Text))
            {
                string[] ExcludeListItems = FilesExcludeTextBox.Text.Split(',');

                if (FilesRegexMatchCheckBox.IsChecked == true)
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

        private void ExitBackupForm_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveRegistryKey_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey _key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Directory\\shell", true);
            _key.DeleteSubKey("CompressFolder\\Command");
            _key.DeleteSubKey("CompressFolder");
            _key.Close();
        }

        private void AddBackupToRegistry_Click(object sender, RoutedEventArgs e)
        {

            RegistryKey rootKey = Registry.CurrentUser;
            RegistryKey subKey = rootKey.CreateSubKey("Software\\Classes\\Directory\\shell\\CompressFolder");
            subKey.SetValue("", "Open in CompressFolder");
            subKey.SetValue("Icon", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            subKey.CreateSubKey("command").SetValue("", "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"" + " \"" + "%1" + "\"");
            subKey.Close();
            rootKey.Close();
        }

        private async void CompressToUSB_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CompressTextBox.Text))
            {
                MessageBox.Show("Please Select Folder.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                CompressTextBox.Focus();
                return;
            }

            string[] FolderNamesArray = CompressTextBox.Text.Split(',');

            var excludeFileList = new List<Regex>();
            var excludeDirList = new List<Regex>();


            if (binFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("bin"));
            if (objFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("obj"));
            if (tmpFolderCheckBox.IsChecked == true) excludeDirList.Add(new Regex("tmp"));

            if (!string.IsNullOrEmpty(FoldersExcludeListTextBox.Text))
            {
                string[] ExcludeListItems = FoldersExcludeListTextBox.Text.Split(',');
                if (FoldersRegexMatchCheckBox.IsChecked == true)
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

            if (!string.IsNullOrEmpty(FilesExcludeTextBox.Text))
            {
                string[] ExcludeListItems = FilesExcludeTextBox.Text.Split(',');

                if (FilesRegexMatchCheckBox.IsChecked == true)
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

            string folderPath = BackupFormDrivesCombo.Text + @"_ASP\BackupFolder\";

            BackupAndCode.BackupClass backupClass = new BackupAndCode.BackupClass();
            backupClass.ZipFolders(folderPath, FolderNamesArray, excludeFileList, excludeDirList);
        }

        private void BackupFormDrivesCombo_MouseEnter(object sender, MouseEventArgs e)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            BackupFormDrivesCombo.Items.Clear();
            foreach (DriveInfo SingleDrive in allDrives)
            {
                if (SingleDrive.DriveType == DriveType.Removable)
                    BackupFormDrivesCombo.Items.Add(SingleDrive.ToString());
            }
            BackupFormDrivesCombo.IsEnabled = true;
        }

        private void FolderFileExecludeClear_Click(object sender, RoutedEventArgs e)
        {
            FoldersExcludeListTextBox.Text = "";
            FilesExcludeTextBox.Text = "";
        }

        private async void ExtractFolders_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ZipFileOpen = new Microsoft.Win32.OpenFileDialog();

            ZipFileOpen.Filter = "ZIP Archive File Format(*.zip)|*.zip|All Files(*.*)|*.*";
            ZipFileOpen.Title = "Open ZIP Archive File Format";

            Regex RemoveDateTimeStamp = new Regex("-[0-9]*-[0-9]*-[0-9]*[ ][0-9]*-[0-9]*-[0-9]*");

            bool? zipopen = ZipFileOpen.ShowDialog();

            if (zipopen == true)
            {
                string ZipFileName = ZipFileOpen.FileName;
                string FolderName = MY_XM_GetFolderName();
                await Task.Run(() =>
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(ZipFileName, Path.Combine(FolderName, (Path.GetFileNameWithoutExtension(ZipFileName))));
                    }

                    catch (System.IO.IOException FileIOException)
                    {
                        MessageBox.Show(FileIOException.Message);
                    }
                });

            }

        }

        private void FoldersExcludeListTextBox_PreviewEnter(object sender, DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.All;
        }

        private void FoldersExcludeListTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;  // Allow copying files
                VisualStateManager.GoToState(this, "DragOver", true); // Apply a visual state for drag-over (optional)
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;  // Don't allow drop if not a file
            }
        }

        private void FoldersExcludeListTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] Dropedfiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            List<string> ManagedFolders = new List<string>();
            foreach (var file in Dropedfiles)
            {
                string FileName = Path.GetFileName(file);
                FoldersExcludeListTextBox.Text += FileName + ",";
            }
        }

        private void FilesExcludeTextBox_PreviewEnter(object sender, DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.All;
        }

        private void FilesExcludeTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;  // Allow copying files
                VisualStateManager.GoToState(this, "DragOver", true); // Apply a visual state for drag-over (optional)
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;  // Don't allow drop if not a file
            }
        }

        private void FilesExcludeTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] Dropedfiles = (string[])e.Data.GetData(DataFormats.FileDrop, false); 
            foreach (var file in Dropedfiles)
            {
                string FileName = Path.GetFileName(file);
                FilesExcludeTextBox.Text += FileName + ",";
            }
        }

        private void CompressWithExclude_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileAttributes DetermineDirectory = System.IO.File.GetAttributes(CompressTextBox.Text.TrimEnd(','));
                if ((DetermineDirectory & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    CreateDummyFilesMethod();
                }
                else
                {
                    MessageBox.Show("Please Drop Only Directories!");
                }
            }
            catch (ArgumentException ArgEx)
            {
                MessageBox.Show(ArgEx.Message + "\nPlease Drop Directory to Compress!");
            }
        }

    }
}