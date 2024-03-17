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
    /// Interaction logic for LinMessageBox.xaml
    /// </summary>
    public partial class LinMessageBox : Window
    {
        public LinMessageBox()
        {
            InitializeComponent();

            this.Owner = App.Current.MainWindow;
        }

        public static void Show(string message)
        { 
            LinMessageBox messageBox = new LinMessageBox();
            messageBox.ShowDialog();
        }
    }
}
