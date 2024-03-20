using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFLinIDE01
{
    /// <summary>
    /// Interaction logic for NameFolderWindow.xaml
    /// </summary>
    public partial class NameFolderWindow : Window
    {
        public string DirectoryPath { get; }
        private Window window;

        public NameFolderWindow(Window window)
        {
            InitializeComponent();
            this.window = window;
            this.Owner = window;

            if (!string.IsNullOrEmpty(App.Current.Properties["FilePath"]?.ToString()))
            {
                DirectoryPath = App.Current.Properties["FilePath"].ToString() + "\\";
            }
            else
            {
                MessageBox.Show("No file path was selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            DataContext = this;
        }

        public void ShowWindow()
        {
            this.Show();
            window.IsHitTestVisible = false;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            window.IsHitTestVisible = true;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Properties["FolderName"] = Path.GetFileNameWithoutExtension(tbFileName.Text);
            this.Close();
        }
    }
}
