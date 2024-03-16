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
    /// Interaction logic for NameFileWindow.xaml
    /// </summary>
    public partial class NameFileWindow : Window
    {
        public string DirectoryPath { get; }

        public NameFileWindow()
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;

            if (!string.IsNullOrEmpty(App.Current.Properties["DotPath"].ToString()))
            {
                DirectoryPath = App.Current.Properties["DotPath"].ToString();
            }
            else
            {
                MessageBox.Show("No file path was selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            DataContext = this;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Properties["FileName"] = Path.GetFileNameWithoutExtension(tbFileName.Text);
            this.Close();
        }
    }
}
