using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BackupFoldersWPF
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application application = new App();
                application.Run(new MainWindow(args));
        }
            catch (IOException IOEx)
            {
                MessageBox.Show("File Cannot Be Accessed\n" + IOEx.Message);
            }
}
    }
}
