using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using WPFLinIDE01.Core;

namespace WPFLinIDE01
{
    /// <summary>
    /// Interaction logic for HomeControl.xaml
    /// </summary>
    public partial class HomeControl : UserControl
    {
        public HomeControl()
        {
            InitializeComponent();
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);

            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a Project",
                InitialDirectory = "C:\\"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                App.Current.Properties["ProjectPath"] = openFolderDialog.FolderName;
                App.Current.Properties["ProjectName"] = Path.GetFileName(openFolderDialog.FolderName);

                MetaDataFile.CreateMetaFile(openFolderDialog.FolderName, Path.GetFileName(openFolderDialog.FolderName));
                // MetaDataFile.SetMetaValue("ProjectName", "Test123");
                Debug.WriteLine(MetaDataFile.GetMetaValue("EditorSettings.IndentationSize"));
                window.Close();
            }

        }
        private void btNewProject_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            
            if (window is HomePage homePage)
            {
                // Access the SetUserControl method of the parent window
                homePage.SetUserControl(new NameProjectControl());
            }
        }
    }
}
