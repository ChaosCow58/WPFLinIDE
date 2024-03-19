using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WPFLinIDE01.Core;


namespace WPFLinIDE01
{
    /// <summary>
    /// Interaction logic for NameProjectControl.xaml
    /// </summary>
    public partial class NameProjectControl : UserControl
    {
        private MetaDataFile metaDataFile;

        public NameProjectControl()
        {
            InitializeComponent();
            metaDataFile = new MetaDataFile();
            App.Current.Properties["MetaData"] = metaDataFile;
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);


            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a Project Location",
                InitialDirectory = "C:\\"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                DirectoryInfo diretoryPath = Directory.CreateDirectory(Path.Combine(openFolderDialog.FolderName, tbProjectName.Text));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "Assets"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "Resources"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "bin"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, @"bin\Logs"));

                metaDataFile.CreateMetaFile(diretoryPath.FullName, tbProjectName.Text);

                metaDataFile.SetMetaValue("ProjectName", tbProjectName.Text);
                metaDataFile.SetMetaValue("ProjectPath", diretoryPath.FullName);

                FileStream basicFile = File.Create(Path.Combine(diretoryPath.FullName, "Program.cs"));

                using (StreamWriter writer = new StreamWriter(basicFile))
                {
                    writer.WriteLine(@$"using System;

namespace {diretoryPath.Name}
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            Console.WriteLine(""Hello, world!"");
        }}
    }}
}}");
                }
                App.Current.Properties["projectOpened"] = true;
                window.Close();
            }
        }

        private void btBack_Click(object sender, RoutedEventArgs e)
        {
            // Obtain the parent window dynamically
            Window window = Window.GetWindow(this);

            if (window is HomePage homePage)
            {
                // Access the SetUserControl method of the parent window
                homePage.SetUserControl(new HomeControl());
            }
        }
    }
}
