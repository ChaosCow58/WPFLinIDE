using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace WPFLinIDE01
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Window
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a Project",
                InitialDirectory = "C:\\"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                App.Current.Properties["ProjectPath"] = openFolderDialog.FolderName;
                App.Current.Properties["ProjectName"] = Path.GetFileName(openFolderDialog.FolderName);
                Close();
            }
            
        }

        private void btNewProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a Project Location",
                InitialDirectory = "C:\\"
            };

            NameProject project = new NameProject();
            project.ShowDialog();

            if (App.Current.Properties["ProjectName"] == null)
            { 
                return;
            }

            if (openFolderDialog.ShowDialog() == true)
            {
                DirectoryInfo diretoryPath = Directory.CreateDirectory(Path.Combine(openFolderDialog.FolderName, App.Current.Properties["ProjectName"].ToString()));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "Assets"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "Resources"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, "bin"));
                Directory.CreateDirectory(Path.Combine(diretoryPath.FullName, @"bin\Logs"));

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

                App.Current.Properties["ProjectPath"] = diretoryPath.FullName;
                App.Current.Properties["ProjectName"] = diretoryPath.Name;

                Close();
            }
           
        }
    }
}
