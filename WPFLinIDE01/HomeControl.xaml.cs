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
        private MetaDataFile metaDataFile;

        public HomeControl()
        {
            InitializeComponent();

            metaDataFile = new MetaDataFile();

            App.Current.Properties["MetaData"] = metaDataFile;
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);

            OpenFileDialog openFolderDialog = new OpenFileDialog()
            {
                Title = "Select a Project",
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = "C:\\",
                Filter = "LinIDE Project Files (*.linproj)|*.linproj"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                string fileDirectory = Path.GetDirectoryName(openFolderDialog.FileName);

                metaDataFile.CreateMetaFile(fileDirectory, Path.GetFileNameWithoutExtension(openFolderDialog.FileName));

                metaDataFile.SetMetaValue("ProjectName", Path.GetFileNameWithoutExtension(openFolderDialog.FileName));
                metaDataFile.SetMetaValue("ProjectPath", fileDirectory);

                App.Current.Properties["projectOpened"] = true;
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
