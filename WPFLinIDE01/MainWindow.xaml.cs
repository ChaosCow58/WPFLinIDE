using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using ConsoleControls = ConsoleControl.ConsoleControl;

namespace WPFLinIDE01
{
    public partial class MainWindow : Window
    {
        HomePage homePage;

        private Process process;
        private WindowsFormsHost host;
        private ConsoleControls terminal;

        public ICommand Show_PowerShell { get; }

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage();
            homePage.ShowDialog();

            if (App.Current.Properties["ProjectPath"] == null)
            { 
                Close();
            }

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            Show_PowerShell = new RelayCommand(ShowPowerShell);

            DataContext = this;
        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                terminal.Dispose();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayFileSystem();
            CreateTermial();
        }

        #region FileExplorer

        private void PopulateTreeView(string path, TreeViewItem parentNode)
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(path))
                {
                    TreeViewItem folderNode = new TreeViewItem();
                    folderNode.Header = Path.GetFileName(folder);
                    parentNode.Items.Add(folderNode);

                    PopulateTreeView(folder, folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                { 
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = Path.GetFileName(file);
                    parentNode.Items.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);    
            }
        }

        private void DisplayFileSystem()
        {
            string rootFolder = App.Current.Properties["ProjectPath"].ToString();

            TreeViewItem rootNode = new TreeViewItem();
            rootNode.Header = App.Current.Properties["ProjectName"].ToString();
            tvFileTree.Items.Add(rootNode);

            PopulateTreeView(rootFolder, rootNode);
        }

        #endregion FileExplorer

        #region Commands
        private void ShowPowerShell(object parameter)
        {
            if (gTermialPanel.Visibility == Visibility.Collapsed)
            {
                CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
                terminal.Focus();
            }
            else if (gTermialPanel.Visibility == Visibility.Visible)
            {
                gTermialPanel.Visibility = Visibility.Collapsed;
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    terminal.Dispose();
                }
            }
        }
        #endregion Commands

        #region Terminal
        private void CreateTermial()
        { 
            process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };

            terminal = new ConsoleControls();

            process.Start();

            terminal.ProcessInterface.StartProcess(process.StartInfo);
            terminal.OnConsoleInput += Terminal_OnConsoleInput;


            terminal.IsInputEnabled = true;
            terminal.Font = new Font("Poppins", 11);
            
            

            terminal.BorderStyle = BorderStyle.FixedSingle;

            host = new WindowsFormsHost();
            host.Child = terminal;

            gTermialPanel.Children.Add(host);
        }

        private void Terminal_OnConsoleInput(object sender, ConsoleControl.ConsoleEventArgs args)
        {
            if (args.Content == "exit")
            {
                // Close the terminal
                Dispatcher.Invoke(() =>
                {
                    gTermialPanel.Children.Remove(host);
                    gTermialPanel.Visibility = Visibility.Collapsed;
                });

                // Kill the process
                if (!process.HasExited)
                {
                    process.Kill();
                    terminal.Dispose();
                }
            }
        }
        #endregion Terminal


    }
}
