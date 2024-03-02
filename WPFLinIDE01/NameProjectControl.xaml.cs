using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace WPFLinIDE01
{
    /// <summary>
    /// Interaction logic for NameProjectControl.xaml
    /// </summary>
    public partial class NameProjectControl : UserControl
    {
        public NameProjectControl()
        {
            InitializeComponent();
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);

            App.Current.Properties["ProjectName"] = tbProjectName.Text;

            if (App.Current.Properties["ProjectName"] == null)
            {
                return;
            }

            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select a Project Location",
                InitialDirectory = "C:\\"
            };

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
