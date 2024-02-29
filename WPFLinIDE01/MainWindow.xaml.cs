using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Newtonsoft.Json;

using ICSharpCode.AvalonEdit.Highlighting;

using WPFLinIDE01.Core;
using ICSharpCode.AvalonEdit;


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

        private FileExporler fileExporler;
        private Terminal cmdTerminal;
        private SyntaxHighlight syntax;

        public ICommand ShowPowerShell_Command { get; }
        public ICommand SaveFile_Command { get; }
        public ICommand RunCode_Command { get; }

        public IHighlightingDefinition SyntaxHighlighting { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage();
            homePage.ShowDialog();

            if (App.Current.Properties["ProjectPath"] == null)
            {
                Close();
            }

            fileExporler = new FileExporler(tvFileTree, tbEditor);
            cmdTerminal = new Terminal(gTermialPanel);
            syntax = new SyntaxHighlight();

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            ShowPowerShell_Command = new RelayCommand(ShowPowerShell);
            SaveFile_Command = new RelayCommand(SaveFile);
            RunCode_Command = new RelayCommand(RunCode);

            lRunCode.Content = $"Run {App.Current.Properties["ProjectName"]}";

            BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/save.png"));
            miSaveItem.Icon = new Image { Source = icon };

            TextEditorOptions options = new TextEditorOptions();
            options.IndentationSize = 3;
            options.ConvertTabsToSpaces = true;
            options.HighlightCurrentLine = true;
            options.EnableHyperlinks = true;
            options.EnableImeSupport = true;
            options.RequireControlModifierForHyperlinkClick = true;
            options.CutCopyWholeLine = true;
            tbEditor.Options = options;

        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cmdTerminal.process != null && !cmdTerminal.process.HasExited)
            {
                cmdTerminal.process.Kill();
                cmdTerminal.terminal.Dispose();
            }
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            fileExporler.DisplayFileSystem();
            cmdTerminal.CreateTermial();

            SyntaxHighlighting = syntax.LoadSyntaxHighlightDefintion("CSSyntaxHighlight.xshd");
            DataContext = this;
        }

        #region Commands

        private void ShowPowerShell(object parameter)
        {
            if (gTermialPanel.Visibility == Visibility.Collapsed)
            {
                cmdTerminal.CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
                tbEditor.MaxHeight = 500;
                cmdTerminal.terminal.Focus();
            }
            else if (gTermialPanel.Visibility == Visibility.Visible)
            {
                gTermialPanel.Visibility = Visibility.Collapsed;
                tbEditor.MaxHeight = 720;
                if (cmdTerminal.process != null && !cmdTerminal.process.HasExited)
                {
                    cmdTerminal.process.Kill();
                    cmdTerminal.terminal.Dispose();
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

            if (!string.IsNullOrEmpty(fileExporler.currentFilePath))
            {
                using (StreamWriter writer = new StreamWriter(fileExporler.currentFilePath))
                {
                    writer.WriteLine(tbEditor.Text);
                    writer.Close();
                }
            }

            TreeViewItem selectedItem = tvFileTree.SelectedItem as TreeViewItem;
            if (!fileExporler.openFile && selectedItem != null)
            {
                TextBlock textBlock = Utility.FindVisualChild<TextBlock>(selectedItem);

                if (textBlock.Text.EndsWith("*"))
                {
                    // Remove the star from the end
                    textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Length - 1);
                }
            }
            fileExporler.openFile = false;


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
                cmdTerminal.CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
                gsTerminalSplitter.Visibility = Visibility.Visible;
                tbEditor.MaxHeight = 500;
                
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
            cmdTerminal.terminal.ProcessInterface.WriteInput(@$"&'{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Roslyn\csc.exe' -out:'{binDirectory}{Path.GetFileNameWithoutExtension(App.Current.Properties["ProjectName"].ToString())}.exe' -debug:full -nologo -errorendlocation -errorlog:'{errorLogDirJson}' '{fileExporler.currentFilePath}'");

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
                    Thread.Sleep(70); // Adjust delay as needed
                }
            }


            if (string.IsNullOrEmpty(Item.level))
            {
                cmdTerminal.terminal.ProcessInterface.WriteInput(@$"&'{binDirectory}{Path.GetFileNameWithoutExtension(App.Current.Properties["ProjectName"].ToString())}.exe'");
            }

            cmdTerminal.terminal.Focus();
            cmdTerminal.terminal.InternalRichTextBox.ScrollToCaret();
        }

    #endregion Commands

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedItem)
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
            if (!fileExporler.openFile && selectedItem != null)
            {
                TextBlock textBlock = Utility.FindVisualChild<TextBlock>(selectedItem);

                if (textBlock != null && !textBlock.Text.EndsWith('*'))
                {
                    textBlock.Text += "*";
                }
            }
            fileExporler.openFile = false;
        }
    }
}
