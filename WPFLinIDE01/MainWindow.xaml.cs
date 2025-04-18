﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

using Newtonsoft.Json;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

using WPFLinIDE01.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Editing;
using System.Management;
using System.Runtime.InteropServices;

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
        private MetaDataFile meta;

        public ICommand ShowPowerShell_Command { get; }
        public ICommand SaveFile_Command { get; }
        public ICommand RunCode_Command { get; }
        public ICommand CopyLine_Command { get; }
        public ICommand MoveLineUp_Command { get; }
        public ICommand MoveLineDown_Command { get; }
        public ICommand Rename_Command { get; }
        public ICommand Delete_Command { get; }

        public IHighlightingDefinition SyntaxHighlighting { get; set; }

        private readonly ListBox listBox;
        private readonly Popup popup;
        private readonly Workspace workspace;

        public MainWindow()
        {
            InitializeComponent();

            listBox = new ListBox();
            popup = new Popup();
            
            listBox.SelectionChanged += ListBox_SelectionChanged;
            listBox.Visibility = Visibility.Collapsed;
            popup.Child = listBox;
            
            Panel.SetZIndex(listBox, 1);

            workspace = new AdhocWorkspace();

            homePage = new HomePage();
            homePage.ShowDialog();


            if (!(bool)App.Current.Properties["projectOpened"])
            {
                this.Close();
            }

            meta = (MetaDataFile)App.Current.Properties["MetaData"];
            fileExporler = new FileExporler(tvFileTree, tcFileTabs, this, meta);
            fileExporler.editor = fileExporler.editor;
            cmdTerminal = new Terminal(gTermialPanel, meta);

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
            Delete_Command = new RelayCommand(DeleteExporler);

            lRunCode.Content = $"Run {meta.GetMetaValue<string>("ProjectName")}";

            //for (int i = 0; i < fileExporler.editor.TextArea.CommandBindings.Count; i++)
            //{
            //    Debug.WriteLine($"Command: {fileExporler.editor.TextArea.CommandBindings[i]}");
            //}


        }

        private List<string> GetCompletionSuggestions(string text, int caretPostion)
        { 
            List<string> suggestions = new List<string>();
            
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxNode root = syntaxTree.GetRoot();
            SyntaxToken token = root.FindToken(caretPostion);

            SyntaxNode memberAccessNode = root.DescendantNodes().Last();

            SemanticModel semanticModel = GetSemanticModel(syntaxTree);
            ITypeSymbol symbolInfo = semanticModel.GetTypeInfo(memberAccessNode).Type;

            if (symbolInfo != null) 
            { 
                foreach (ISymbol symbol in symbolInfo.GetMembers()) 
                {
                    if (!symbol.CanBeReferencedByName || symbol.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public || symbol.IsStatic)
                    {
                        continue;
                    }

                    suggestions.Add(symbol.Name);
                }
            
            }



            // Debug.WriteLine($"Token: {token.Kind()}");

            return suggestions;
            
        }

        private SemanticModel GetSemanticModel(SyntaxTree syntaxTree) 
        {
            CSharpCompilation compilation = CSharpCompilation.Create("WPFLinIDE01")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);

            return compilation.GetSemanticModel(syntaxTree);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectSuggestion = (string)listBox.SelectedItem;

            if (selectSuggestion != null)
            {
                fileExporler.editor.Document.Insert(fileExporler.editor.CaretOffset, selectSuggestion);
            }

            listBox.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cmdTerminal.process != null && !cmdTerminal.process.HasExited)
            {
                cmdTerminal.process.Kill();
                cmdTerminal.terminal.Dispose();
            }

            // MetaDataFile.SetMetaValue("Settings", "", true);
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
        private void miSave_Click(object sender, RoutedEventArgs e)
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

        private int processId = 0;

        private void RunCodeBase()
        {
            if (gTermialPanel.Visibility == Visibility.Collapsed && gsTerminalSplitter.Visibility == Visibility.Collapsed)
            {
                cmdTerminal.CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
                gsTerminalSplitter.Visibility = Visibility.Visible;
                fileExporler.editor.MaxHeight = 500;

            }

            Thread.Sleep(500);

            string binDirectory = @$"{meta.GetMetaValue<string>("ProjectPath")}\bin\";
            string errorLogDir = @$"{binDirectory}Logs\";
            string errorLogDirJson = @$"{binDirectory}Logs\{Path.GetFileNameWithoutExtension(meta.GetMetaValue<string>("ProjectName"))}.json";

            if (!Directory.Exists(binDirectory))
            {
                Directory.CreateDirectory(binDirectory);
            }

            if (!Directory.Exists(errorLogDir))
            {
                Directory.CreateDirectory(errorLogDir);
            }

            SaveFileBase();

            try
            {
                if (processId != 0 && !Process.GetProcessById(processId).HasExited)
                {
                    Process.GetProcessById(processId).Kill();
                    processId = 0;
                }
            }
            catch (ArgumentException ex)
            { 
                Debug.WriteLine(ex.Message);
            }

            Thread.Sleep(250);

            // TODO make a checkbox for unsafe mode use -unsafe if true
            cmdTerminal.terminal.ProcessInterface.WriteInput(@$"&'{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Roslyn\csc.exe' -out:'{binDirectory}{Path.GetFileNameWithoutExtension(meta.GetMetaValue<string>("ProjectName"))}.exe' -debug:full -nologo -errorendlocation -errorlog:'{errorLogDirJson}' '{fileExporler.currentFilePath}'");

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
                string exeCommand = @$"""{binDirectory}{Path.GetFileNameWithoutExtension(meta.GetMetaValue<string>("ProjectName"))}.exe""";
                string styleCommand = @$"""{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\style.ps1""";

                Process outputProcess = new Process();
                outputProcess.StartInfo = new ProcessStartInfo() 
                { 
                    FileName = "conhost",
                    Arguments = $"powershell -NoLogo -Command {styleCommand} 'LinIDE - Running {meta.GetMetaValue<string>("ProjectName")}'; {exeCommand}",
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };

                outputProcess.Start();
                processId = outputProcess.Id;
            }

            cmdTerminal.terminal.Focus();
            cmdTerminal.terminal.InternalRichTextBox.ScrollToCaret();
        }

        private void CopyLine(object parameter)
        {
            int currentLineNumber = fileExporler.editor.TextArea.Caret.Line;

            string currentLineText = fileExporler.editor.Document.GetText(fileExporler.editor.Document.GetLineByNumber(currentLineNumber));
            fileExporler.editor.Document.Insert(fileExporler.editor.Document.GetLineByNumber(currentLineNumber).EndOffset, "\n" + currentLineText);
        }

        private void MoveLineUp(object parameter)
        {
            int caretOffset = fileExporler.editor.CaretOffset;
            int lineNumber = fileExporler.editor.Document.GetLineByOffset(caretOffset).LineNumber;

            if (lineNumber > 1)
            {
                DocumentLine line = fileExporler.editor.Document.GetLineByNumber(lineNumber);
                string lineText = fileExporler.editor.Document.GetText(line);
                DocumentLine prevLine = fileExporler.editor.Document.GetLineByNumber(lineNumber - 1);

                fileExporler.editor.Document.Remove(line.Offset, line.TotalLength);
                fileExporler.editor.Document.Insert(prevLine.Offset, lineText + Environment.NewLine);

                // Move caret to the beginning of the moved line
                fileExporler.editor.CaretOffset = prevLine.Offset;
            }
        }

        private void MoveLineDown(object parameter)
        {
            int caretOffset = fileExporler.editor.CaretOffset;
            int lineNumber = fileExporler.editor.Document.GetLineByOffset(caretOffset).LineNumber;
            int totalLines = fileExporler.editor.Document.LineCount;

            if (lineNumber < totalLines - 1)
            {
                var line = fileExporler.editor.Document.GetLineByNumber(lineNumber);
                var lineText = fileExporler.editor.Document.GetText(line);
                var nextLine = fileExporler.editor.Document.GetLineByNumber(lineNumber + 1);

                // Remove the newline character at the end of the lineText if it exists
                if (lineText.EndsWith(Environment.NewLine))
                {
                    lineText = lineText.Substring(0, lineText.Length - Environment.NewLine.Length);
                }

                fileExporler.editor.Document.Remove(line.Offset, line.TotalLength);
                fileExporler.editor.Document.Insert(nextLine.Offset, lineText);

                // Move caret to the beginning of the moved line
                fileExporler.editor.CaretOffset = nextLine.Offset;
            }
        }

        private void RenameExporler(object parameter)
        {
            fileExporler.ItemRenameMenuItemBase();
        }

        private void DeleteExporler(object parameter)
        {
            fileExporler.IteDeleteMenuItemBase();
        }

        #endregion Commands

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
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


            string text = fileExporler.editor.Text;
            int caretPostion = fileExporler.editor.CaretOffset;

            List<string> suggestions = GetCompletionSuggestions(text, caretPostion);

            listBox.ItemsSource = suggestions;
            listBox.Visibility = suggestions.Any() ? Visibility.Visible : Visibility.Collapsed;

            listBox.Margin = new Thickness(fileExporler.editor.Margin.Left + caretPostion,
                fileExporler.editor.Margin.Top + caretPostion, 0, 0);

            Caret caret = fileExporler.editor.TextArea.Caret;
            TextView textView = fileExporler.editor.TextArea.TextView;
            // var location = textView.GetVisualPosition(caret.VisualColumn, caret.Line);

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

        private void bMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("WPFLinIDE01");
            Process.Start(processStartInfo);
        }


    } // Class
} // Namespace
