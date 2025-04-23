using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BackupFoldersWPF.Controls
{
    /// <summary>
    /// Interaction logic for RegestryUserControl.xaml
    /// </summary>
    public partial class RegestryUserControl : UserControl
    {
        public RegestryUserControl()
        {
            InitializeComponent();
        }

        private void RemoveRegistryKey_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey _key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\Directory\\shell", true);
            _key.DeleteSubKey("BackupFolderWPF\\Command");
            _key.DeleteSubKey("BackupFolderWPF");
            _key.Close();
        }

        private void AddBackupToRegistry_Click(object sender, RoutedEventArgs e)
        {

            RegistryKey rootKey = Registry.CurrentUser;
            RegistryKey subKey = rootKey.CreateSubKey("Software\\Classes\\Directory\\shell\\BackupFolderWPF");
            subKey.SetValue("", "Open in BackupFolderWPF");
            subKey.SetValue("Icon", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            subKey.CreateSubKey("command").SetValue("", "\"" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"" + " \"" + "%1" + "\"");
            subKey.Close();
            rootKey.Close();
        }
    }
}
