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
    }
}
