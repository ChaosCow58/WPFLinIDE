using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

        private string currentFilePath = string.Empty;

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
                    folderNode.Header = CreateHeader(Path.GetFileName(folder), true); // Indicate it's a folder
                    folderNode.Tag = folder; // Store full path for later use
                    folderNode.Items.Add("*"); // Placeholder to show expand/collapse arrow
                    folderNode.Expanded += Folder_Expanded; // Attach event for lazy loading
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
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
            rootNode.Header = CreateHeader(App.Current.Properties["ProjectName"].ToString(), true); // Indicate it's a folder
            tvFileTree.Items.Add(rootNode);

            PopulateTreeView(rootFolder, rootNode);
        }

        private StackPanel CreateHeader(string text, bool isFolder)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = System.Windows.Controls.Orientation.Horizontal;

            if (isFolder)
            {
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Resources/folder.png")); // Set your folder icon path here
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }
            else
            {
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Resources/file.png"));
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }

            TextBlock headerText = new TextBlock();
            headerText.Text = text;
            panel.Children.Add(headerText);

            return panel;
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem folderNode = (TreeViewItem)sender;
            if (folderNode.Items.Count == 1 && (string)folderNode.Items[0] == "*") // Lazy loading
            {
                folderNode.Items.Clear();
                string path = (string)folderNode.Tag;
                try
                {
                    foreach (string folder in Directory.GetDirectories(path))
                    {
                        TreeViewItem subFolderNode = new TreeViewItem();
                        subFolderNode.Header = CreateHeader(Path.GetFileName(folder), true);
                        subFolderNode.Tag = folder;
                        subFolderNode.Items.Add("*"); // Placeholder for sub-nodes
                        subFolderNode.Expanded += Folder_Expanded; // Attach event for lazy loading
                        folderNode.Items.Add(subFolderNode);
                    }

                    foreach (string file in Directory.GetFiles(path))
                    {
                        TreeViewItem fileNode = new TreeViewItem();
                        fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                        fileNode.MouseDoubleClick += FileNode_MouseDown;
                        folderNode.Items.Add(fileNode);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FileNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = sender as TreeViewItem;

            try
            {
                currentFilePath = treeViewItem.Tag.ToString();

                tbEditor.Text = File.ReadAllText(currentFilePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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

            terminal.ProcessInterface.WriteInput($"cd {App.Current.Properties["ProjectPath"]}");

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

        private void btRunCode_Click(object sender, RoutedEventArgs e)
        {
          
            if (gTermialPanel.Visibility == Visibility.Collapsed)
            {
                CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
            }
            Thread.Sleep(500);

            string binDirectory = @$"{App.Current.Properties["ProjectPath"]}\bin\";

            if (!Directory.Exists(binDirectory))
            { 
                Directory.CreateDirectory(binDirectory);
            }

            terminal.ProcessInterface.WriteInput(@$"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Roslyn\csc.exe -out:""{binDirectory}{Path.GetFileNameWithoutExtension(currentFilePath)}.exe"" ""{currentFilePath}""");
            terminal.ProcessInterface.WriteInput(@$"{binDirectory}{Path.GetFileNameWithoutExtension(currentFilePath)}.exe");
            
        }
    }
}
