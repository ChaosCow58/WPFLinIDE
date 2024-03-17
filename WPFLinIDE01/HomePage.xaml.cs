using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

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
            SetUserControl(new HomeControl());

            App.Current.Properties["projectOpened"] = false;
        }

        public void SetUserControl(UserControl userControl)
        {
           ccHomePanel.Children.Clear();
           ccHomePanel.Children.Add(userControl);
        }

        private void btMimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btMaximze_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bTopBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            { 
                this.DragMove();
            }
        }
    }
}
