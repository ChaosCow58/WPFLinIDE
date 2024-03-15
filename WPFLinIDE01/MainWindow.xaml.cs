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
using ICSharpCode.AvalonEdit.Document;

#pragma warning disable CA1416

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
        public ICommand CopyLine_Command { get; }
        public ICommand MoveLineUp_Command { get; }
        public ICommand MoveLineDown_Command { get; }
        public ICommand Rename_Command { get; }

        public IHighlightingDefinition SyntaxHighlighting { get; set; }
        private TextEditor tbEditor;

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage();
            homePage.ShowDialog();

            if (App.Current.Properties["ProjectPath"] == null)
            {
                Close();
            }

            fileExporler = new FileExporler(tvFileTree, tcFileTabs);
            tbEditor = fileExporler.editor;
            cmdTerminal = new Terminal(gTermialPanel);

            syntax = new SyntaxHighlight();

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            ShowPowerShell_Command = new RelayCommand(ShowPowerShell);
            SaveFile_Command = new RelayCommand(SaveFile);
            RunCode_Command = new RelayCommand(RunCode);
            CopyLine_Command = new RelayCommand(CopyLine);
            MoveLineUp_Command = new RelayCommand(MoveLineUp);
            MoveLineDown_Command = new RelayCommand(MoveLineDown);
            Rename_Command = new RelayCommand(RenameExporler);

            lRunCode.Content = $"Run {App.Current.Properties["ProjectName"]}";

            BitmapImage icon = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/save.png"));
            miSaveItem.Icon = new Image { Source = icon };


            //for (int i = 0; i < tbEditor.TextArea.CommandBindings.Count; i++)
            //{
            //    Debug.WriteLine($"Command: {tbEditor.TextArea.CommandBindings[i]}");
            //}
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
                fileExporler.editor.MaxHeight = 500;
                cmdTerminal.terminal.Focus();
            }
            else if (gTermialPanel.Visibility == Visibility.Visible)
            {
                gTermialPanel.Visibility = Visibility.Collapsed;
                fileExporler.editor.MaxHeight = 720;
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
            TextBlock block = new TextBlock();

            if (!string.IsNullOrEmpty(fileExporler.currentFilePath))
            {
                using (StreamWriter writer = new StreamWriter(fileExporler.currentFilePath))
                {
                    writer.WriteLine(fileExporler.editor.Text);
                    writer.Close();
                }
            }

            TabItem tabItem = (TabItem)fileExporler.tabControl.SelectedItem;
            if (tabItem != null)
            {
                StackPanel stack = Utility.FindVisualChild<StackPanel>(tabItem);

                if (stack != null)
                { 
                    block = Utility.FindVisualChild<TextBlock>(stack);
                }
            }

            foreach (ExplorlerTreeViewItem selectedItem in fileExporler.treeview.Items)
            {
                if (selectedItem.HasItems)
                {  
                    foreach (ExplorlerTreeViewItem item in selectedItem.Items) 
                    {   
                        if (!fileExporler.openFile)
                        {
                            TextBox textBlock = Utility.FindVisualChild<TextBox>(item);

                            if (textBlock.Text.Trim('*') == block.Text.Trim('*') && textBlock.Text.EndsWith("*"))
                            {
                                // Remove the star from the end
                                textBlock.Text = textBlock.Text.Substring(0, textBlock.Text.Length - 1);
                                block.Text = block.Text.Substring(0, block.Text.Length - 1);
                                fileExporler.openFile = false;


                                Debug.WriteLine("Save");

                                break;
                            }
                        }
                    }
                }
            }
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


        private void CopyLine(object parameter)
        {
            int currentLineNumber = tbEditor.TextArea.Caret.Line;

            string currentLineText = tbEditor.Document.GetText(tbEditor.Document.GetLineByNumber(currentLineNumber));
            tbEditor.Document.Insert(tbEditor.Document.GetLineByNumber(currentLineNumber).EndOffset, "\n" + currentLineText);
        }

        private void MoveLineUp(object parameter)
        {
            int caretOffset = tbEditor.CaretOffset;
            int lineNumber = tbEditor.Document.GetLineByOffset(caretOffset).LineNumber;

            if (lineNumber > 1)
            {
                DocumentLine line = tbEditor.Document.GetLineByNumber(lineNumber);
                string lineText = tbEditor.Document.GetText(line);
                DocumentLine prevLine = tbEditor.Document.GetLineByNumber(lineNumber - 1);

                tbEditor.Document.Remove(line.Offset, line.TotalLength);
                tbEditor.Document.Insert(prevLine.Offset, lineText + Environment.NewLine);

                // Move caret to the beginning of the moved line
                tbEditor.CaretOffset = prevLine.Offset;
            }
        }

        private void MoveLineDown(object parameter) 
        {
            int caretOffset = tbEditor.CaretOffset;
            int lineNumber = tbEditor.Document.GetLineByOffset(caretOffset).LineNumber;
            int totalLines = tbEditor.Document.LineCount;

            if (lineNumber < totalLines - 1)
            {
                var line = tbEditor.Document.GetLineByNumber(lineNumber);
                var lineText = tbEditor.Document.GetText(line);
                var nextLine = tbEditor.Document.GetLineByNumber(lineNumber + 1);

                // Remove the newline character at the end of the lineText if it exists
                if (lineText.EndsWith(Environment.NewLine))
                {
                    lineText = lineText.Substring(0, lineText.Length - Environment.NewLine.Length);
                }

                tbEditor.Document.Remove(line.Offset, line.TotalLength);
                tbEditor.Document.Insert(nextLine.Offset, lineText);

                // Move caret to the beginning of the moved line
                tbEditor.CaretOffset = nextLine.Offset;
            }
        }

        private void RenameExporler(object parameter) 
        {
            fileExporler.ItemRenameMenuItemBase();
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
       /*     ExplorlerTreeViewItem selectedItem = (ExplorlerTreeViewItem)tvFileTree.SelectedItem;

            StackPanel stackPanel = Utility.FindVisualChild<StackPanel>(selectedItem);
            stackPanel.ContextMenu.Visibility = Visibility.Hidden;*/
        }

        public void tbEditor_TextChanged(object sender, EventArgs e)
        {
            TabItem tabItem = (TabItem)fileExporler.tabControl.SelectedItem;
            if (tabItem != null)
            {
                StackPanel stack = Utility.FindVisualChild<StackPanel>(tabItem);

                if (stack != null)
                {
                    TextBlock textBlock = Utility.FindVisualChild<TextBlock>(stack);

                    if (textBlock != null && !textBlock.Text.EndsWith('*'))
                    {
                        textBlock.Text += "*";
                    }
                }
            }


            ExplorlerTreeViewItem selectedItem = (ExplorlerTreeViewItem)tvFileTree.SelectedItem;
            if (!fileExporler.openFile && selectedItem != null)
            {
                TextBox textBlock = Utility.FindVisualChild<TextBox>(selectedItem);

                if (selectedItem.ItemType == ItemType.File && textBlock != null && !textBlock.Text.EndsWith('*'))
                {
                    textBlock.Text += "*";
                }
            }
            fileExporler.openFile = false;
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = (Button)sender;
            TabItem tabItem = (TabItem)Utility.FindAncestor(closeButton, typeof(TabItem));

            if (tabItem != null)
            {
               fileExporler.tabControl.Items.Remove(tabItem);
            }
        }
    }
}
