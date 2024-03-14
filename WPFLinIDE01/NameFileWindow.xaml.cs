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
using System.Windows.Shapes;

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
            DirectoryPath = "Test123";

            DataContext = this;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
