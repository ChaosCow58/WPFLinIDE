using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using ConsoleControls = ConsoleControl.ConsoleControl;

using Newtonsoft.Json;

using ICSharpCode.AvalonEdit.Highlighting;

using WPFLinIDE01.Core;


namespace WPFLinIDE01
{
    public static class Item
    {
        public static string ruleId { get; set; }
        public static string level { get; set; }
        public static string message { get; set; }
    }

    public partial class MainWindow : Window
    {
        private HomePage homePage;
        private SyntaxHighlight syntax;

        private Process process;
        private WindowsFormsHost host;
        private ConsoleControls terminal;

        public ICommand ShowPowerShell_Command { get; }
        public ICommand SaveFile_Command { get; }
        public ICommand RunCode_Command { get; }

        public IHighlightingDefinition SyntaxHighlighting { get; set; }

        private string currentFilePath = string.Empty;
        private bool openFile = false;

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage();
            homePage.ShowDialog();

            if (App.Current.Properties["ProjectPath"] == null)
            {
                Close();
            }

            syntax = new SyntaxHighlight();

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            ShowPowerShell_Command = new RelayCommand(ShowPowerShell);
            SaveFile_Command = new RelayCommand(SaveFile);
            RunCode_Command = new RelayCommand(RunCode);

            lRunCode.Content = $"Run {App.Current.Properties["ProjectName"]}";

            BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/save.png"));
            miSaveItem.Icon = new System.Windows.Controls.Image { Source = icon };

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
            SyntaxHighlighting = syntax.LoadSyntaxHighlightDefintion("CSSyntaxHighlight.xshd");
            DataContext = this;
        }

        #region FileExplorer

        private void PopulateTreeView(string path, TreeViewItem parentNode)
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(path))
                {
                    if (Path.GetFileName(folder) == "bin" || Path.GetFileName(folder) == "obj")
                    {
                        continue;
                    }

                    TreeViewItem folderNode = new TreeViewItem();
                    folderNode.Header = CreateHeader(Path.GetFileName(folder), true); // Indicate it's a folder
                    folderNode.Tag = folder; // Store full path for later use
                    folderNode.Items.Add("*"); // Placeholder to show expand/collapse arrow
                    folderNode.Expanded += Folder_Expanded; // Attach event for lazy loading
                    folderNode.ContextMenu = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_Folder");
                    folderNode.Selected += FileNode_Selected;
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
                    fileNode.ContextMenu = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_File");
                    fileNode.Selected += FileNode_Selected;
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
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/folder.png")); // Set your folder icon path here
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }
            else
            {
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/file.png"));
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }

            TextBlock headerText = new TextBlock();
            headerText.Foreground = System.Windows.Media.Brushes.White;
            headerText.Text = text;
            panel.Children.Add(headerText);

            return panel;
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem folderNode = (TreeViewItem)sender;
            try
            {
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
                            subFolderNode.ContextMenu = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_Folder");
                            subFolderNode.Selected += FileNode_Selected;
                            folderNode.Items.Add(subFolderNode);
                        }

                        foreach (string file in Directory.GetFiles(path))
                        {
                            TreeViewItem fileNode = new TreeViewItem();
                            fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                            fileNode.Tag = file; // Store full path for later use
                            fileNode.MouseDoubleClick += FileNode_MouseDown;
                            fileNode.ContextMenu = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_Folder");
                            fileNode.Selected += FileNode_Selected;
                            folderNode.Items.Add(fileNode);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void FileNode_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)tvFileTree.SelectedItem;

            if (treeViewItem != null)
            {
                if (Path.HasExtension(Path.GetFileName(treeViewItem.Tag.ToString())))
                {
                    if (Mouse.RightButton == MouseButtonState.Pressed)
                    {
                        ContextMenu context = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_File");
                        if (context != null)
                        {
                            context.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    ContextMenu context = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_Folder");
                    if (context != null)
                    {
                        context.Visibility = Visibility.Visible;
                    }
                }
            }
            e.Handled = true;
        }

        private void FileNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            openFile = true;

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

            if (gsTerminalSplitter.Visibility == Visibility.Collapsed)
            {
                gsTerminalSplitter.Visibility = Visibility.Visible;
            }
            else if (gsTerminalSplitter.Visibility == Visibility.Visible)
            {
                gsTerminalSplitter.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveFile(object parameter)
        {
            SaveFileBase();
        }
        private void miSave_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileBase();
        }

        private void SaveFileBase()
        {

            if (!string.IsNullOrEmpty(currentFilePath))
            {
                using (StreamWriter writer = new StreamWriter(currentFilePath))
                {
                    writer.WriteLine(tbEditor.Text);
                    writer.Close();
                }
            }

            TreeViewItem selectedItem = tvFileTree.SelectedItem as TreeViewItem;
            if (!openFile && selectedItem != null)
            {
                TextBlock textBlock = Utility.FindVisualChild<TextBlock>(selectedItem);

                if (textBlock.Text.EndsWith("*"))
                {
                    // Remove the star from the end
                    textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Length - 1);
                }
            }
            openFile = false;


            Debug.WriteLine("Save");
        }

        private void RunCode(object parameter)
        {
            RunCodeBase();
        }


        private void btRunCode_Click(object sender, RoutedEventArgs e)
        {
            RunCodeBase();
        }

        private void RunCodeBase()
        {
            if (gTermialPanel.Visibility == Visibility.Collapsed && gsTerminalSplitter.Visibility == Visibility.Collapsed)
            {
                CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
                gsTerminalSplitter.Visibility = Visibility.Visible;
            }



            Thread.Sleep(500);

            string binDirectory = @$"{App.Current.Properties["ProjectPath"]}\bin\";
            string errorLogDir = @$"{binDirectory}Logs\";
            string errorLogDirJson = @$"{binDirectory}Logs\{Path.GetFileNameWithoutExtension(App.Current.Properties["ProjectName"].ToString())}.json";

            if (!Directory.Exists(binDirectory))
            {
                Directory.CreateDirectory(binDirectory);
            } 
            
            if (!Directory.Exists(errorLogDir))
            {
                Directory.CreateDirectory(errorLogDir);
            }


            // TODO make a checkbox for unsafe mode use -unsafe if true -errorendlocation
            terminal.ProcessInterface.WriteInput(@$"&'{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Roslyn\csc.exe' -out:'{binDirectory}{Path.GetFileNameWithoutExtension(App.Current.Properties["ProjectName"].ToString())}.exe' -debug:full -nologo -errorendlocation -errorlog:'{errorLogDirJson}' '{currentFilePath}'");

            Thread.Sleep(200);

            int maxAttempts = 10;
            int attempt = 0;
            bool fileReady = false;

            while (!fileReady && attempt < maxAttempts)
            {
                try
                {
                    // Attempt to read the file
                    dynamic data = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(errorLogDirJson));

                    // If reading succeeds, set fileReady to true to break out of the loop
                    fileReady = true;

                    foreach (dynamic obj in data.runs)
                    {
                        if (obj.results.Count == 0)
                        {
                            // Handle the case when there are no results
                            Item.ruleId = null;
                            Item.level = null;
                            Item.message = null;
                            break;
                        }

                        foreach (dynamic result in obj.results)
                        {
                            // Process each result
                            Item.ruleId = result.ruleId;
                            Item.level = result.level;
                            Item.message = result.message;
                        }
                    }
                }
                catch (IOException)
                {
                    // If an IOException occurs, the file is still being used by another process
                    // Retry after a short delay
                    attempt++;
                    System.Threading.Thread.Sleep(70); // Adjust delay as needed
                }
            }


            if (string.IsNullOrEmpty(Item.level))
            { 
               terminal.ProcessInterface.WriteInput(@$"&'{binDirectory}{Path.GetFileNameWithoutExtension(App.Current.Properties["ProjectName"].ToString())}.exe'");
            }

            terminal.Focus();
            terminal.InternalRichTextBox.ScrollToCaret();
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

            terminal.ProcessInterface.WriteInput($"cd \"{App.Current.Properties["ProjectPath"]}\"");

            terminal.IsInputEnabled = true;
            terminal.AutoScroll = true;
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
            else if (args.Content == "clear" || args.Content == "cls")
            {
                terminal.ClearOutput();
            }
        }
        #endregion Terminal

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button clickedItem)
            {
                if (clickedItem.ContextMenu != null && clickedItem.ContextMenu.IsOpen == false)
                {
                    clickedItem.ContextMenu.IsOpen = true;
                }
            }
            e.Handled = true;
        }

        private void ItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ItemRenameMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu context = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_File");
            ContextMenu context1 = (ContextMenu)tvFileTree.FindResource("ItemContextMenu_Folder");

            context.Visibility = Visibility.Hidden;
            context1.Visibility = Visibility.Hidden;
        }

        private void tbEditor_TextChanged(object sender, EventArgs e)
        {
            TreeViewItem selectedItem = tvFileTree.SelectedItem as TreeViewItem;
            if (!openFile && selectedItem != null)
            {
                TextBlock textBlock = Utility.FindVisualChild<TextBlock>(selectedItem);

                if (textBlock != null && !textBlock.Text.EndsWith('*'))
                {
                    textBlock.Text += "*";
                }
            }
            openFile = false;
        }
    }
}
