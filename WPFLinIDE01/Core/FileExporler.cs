using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ICSharpCode.AvalonEdit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace WPFLinIDE01.Core
{
    public class FileExporler
    {
        private TreeView treeview;
        public TextEditor editor;
        private TabControl tabControl;

        public bool openFile = false;
        public string currentFilePath = string.Empty;

        public FileExporler(TreeView treeView, TabControl tabControl)
        {
            this.treeview = treeView;
            this.tabControl = tabControl;
        }

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
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
                    fileNode.LostFocus += FileNode_LostFocus;
                    fileNode.GotFocus += FileNode_GotFocus;
                    parentNode.Items.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void FileNode_GotFocus(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)sender;

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
            if (stack != null)
            { 
                stack.Background = Brushes.Gray;
            }
        }

        private void FileNode_LostFocus(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)sender;

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
            if (stack != null)
            {
                stack.Background = Brushes.DarkGray;
            }
        }

        public void DisplayFileSystem()
        {
            string rootFolder = App.Current.Properties["ProjectPath"].ToString();

            TreeViewItem rootNode = new TreeViewItem();
            rootNode.Header = CreateHeader(App.Current.Properties["ProjectName"].ToString(), true); // Indicate it's a folder
            treeview.Items.Add(rootNode);

            PopulateTreeView(rootFolder, rootNode);
        }

        private StackPanel CreateHeader(string text, bool isFolder)
        {
            StackPanel stackPanel = new StackPanel() 
            {
                Background = Brushes.Transparent,
                Orientation = Orientation.Horizontal,
            };


            ContextMenu menu = new ContextMenu() 
            { 
                Background = Brushes.White,
            };

            menu.MouseRightButtonDown += (sender, e) => 
            {
                Debug.WriteLine("b");
                e.Handled = true;
                menu.Focus();
            };

            if (isFolder)
            {
                Image image = new Image();
                MenuItem[] miFolderItems = {
                    new MenuItem()
                    {
                        Header = "Add",
                        Foreground = Brushes.Black,
                        Command = new RelayCommand(ItemAddMenuItem_Click)
                    },

                    new MenuItem()
                    {
                        Header = "Rename",
                        InputGestureText = "F2",
                        Foreground = Brushes.Black,
                        Command = new RelayCommand(ItemRenameMenuItem_Click)
                    },

                    new MenuItem()
                    {
                        Header = "Delete",
                        InputGestureText = "Del",
                        Foreground = Brushes.Black,
                        Command = new RelayCommand(ItemDeleteMenuItem_Click)
                    }
                    
                };

                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/folder.png")); // Set your folder icon path here
                image.Width = 16;
                image.Height = 16;
               // miFolder.Icon = image;

                TextBox textBlock = new TextBox()
                {
                    Text = text,
                    Foreground = Brushes.White,
                    IsReadOnly = true,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsHitTestVisible = false,
                };

                stackPanel.Children.Add(image);
                stackPanel.Children.Add(textBlock);

                foreach (MenuItem mi in miFolderItems)
                { 
                    menu.Items.Add(mi);
                }
            }
            else
            {
                Image image = new Image();

                MenuItem[] miFileItems = {
                    new MenuItem()
                    {
                        Header = "Rename",
                        Foreground = Brushes.Black,
                        InputGestureText = "F2",
                        Command = new RelayCommand(ItemRenameMenuItemFile_Click)
                    },
                    new MenuItem()
                    {
                        Header = "Delete",
                        Foreground= Brushes.Black,
                        InputGestureText = "Del",
                        Command = new RelayCommand(ItemDeleteMenuItem_Click)
                    }

                };
                  
                   
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/file.png"));
                image.Width = 16;
                image.Height = 16;

                TextBox textBlock = new TextBox() 
                { 
                    Text = text,
                    Foreground = Brushes.White,
                    IsReadOnly = true,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsHitTestVisible = false,
                };

                stackPanel.Children.Add(image);
                stackPanel.Children.Add(textBlock);

                foreach (MenuItem mi in miFileItems) 
                {
                    menu.Items.Add(mi);
                }

            }
      
            
            stackPanel.ContextMenu = menu;
            return stackPanel;
        }

        private void ItemDeleteMenuItem_Click(object parameter)
        {
            Debug.WriteLine("Delete Folder");
        }

        private void ItemRenameMenuItem_Click(object parameter)
        {
            TreeViewItem treeViewItem = (TreeViewItem)treeview.SelectedItem;

            StackPanel stackPanel = Utility.FindVisualChild<StackPanel>(treeViewItem);

            if (stackPanel != null)
            {
                TextBox textBox = Utility.FindVisualChild<TextBox>(stackPanel);
                textBox.IsReadOnly = false;
                textBox.Foreground = Brushes.Black;
                textBox.Background = Brushes.White;
                textBox.CaretBrush = Brushes.Black;
                textBox.IsHitTestVisible = true;

                textBox.Focus();

                textBox.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        string newDirectoryName = textBox.Text.Trim();
                        string currentDirectory = treeViewItem.Tag.ToString();
                        string parentDirectory = Path.GetDirectoryName(currentDirectory);
                        string newDirectoryPath = Path.Combine(parentDirectory, newDirectoryName);

                        try
                        {
                            if (Directory.Exists(currentDirectory))
                            {
                                Directory.Move(currentDirectory, newDirectoryPath);
                                treeViewItem.Tag = newDirectoryPath;

                                // Update UI elements
                                textBox.IsReadOnly = true;
                                textBox.Foreground = Brushes.White;
                                textBox.Background = Brushes.Transparent;
                                textBox.IsHitTestVisible = false;

                                // Optionally update TabItem header
                                /*TabItem tabItem = tabControl.SelectedItem as TabItem;
                                tabItem.Header = newDirectoryName;
                                tabItem.Tag = newDirectoryPath;*/
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error renaming directory: " + ex.Message);
                        }
                    }
                };
            }
        }   
        
        private void ItemRenameMenuItemFile_Click(object parameter)
        {
            TreeViewItem treeViewItem = (TreeViewItem)treeview.SelectedItem;

            StackPanel stackPanel = Utility.FindVisualChild<StackPanel>(treeViewItem);

            if (stackPanel != null)
            {
                TextBox textBox = Utility.FindVisualChild<TextBox>(stackPanel);
                textBox.IsReadOnly = false;
                textBox.Foreground = Brushes.Black;
                textBox.Background = Brushes.White;
                textBox.CaretBrush = Brushes.Black;
                textBox.IsHitTestVisible = true;

                textBox.Focus();

                textBox.KeyDown += (sender, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        string newFile = @$"{Path.GetDirectoryName(treeViewItem.Tag.ToString())}\{textBox.Text}";

                        Debug.WriteLine(Path.GetDirectoryName(treeViewItem.Tag.ToString()));
                        Debug.WriteLine(treeViewItem.Tag);
                        File.Move(treeViewItem.Tag.ToString(), newFile);
                        treeViewItem.Tag = newFile;

                        textBox.IsReadOnly = true;
                        textBox.Foreground = Brushes.White;
                        textBox.Background = Brushes.Transparent;
                        textBox.IsHitTestVisible = false;

                        TabItem tabItem = tabControl.SelectedItem as TabItem;
                        tabItem.Header = Path.GetFileName(treeViewItem.Tag.ToString());
                        tabItem.Tag = newFile;
                    }
                };
            }
        }

        private void ItemAddMenuItem_Click(object parameter) 
        {
            Debug.WriteLine("Add Folder");
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
                            folderNode.Items.Add(subFolderNode);
                        }

                        foreach (string file in Directory.GetFiles(path))
                        {
                            TreeViewItem fileNode = new TreeViewItem();
                            fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                            fileNode.Tag = file; // Store full path for later use
                            fileNode.MouseDoubleClick += FileNode_MouseDown;
                            fileNode.LostFocus += FileNode_LostFocus;
                            fileNode.GotFocus += FileNode_GotFocus;
                            folderNode.Items.Add(fileNode);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


#pragma warning disable CA1416
        private void FileNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            openFile = true;

            TreeViewItem treeViewItem = sender as TreeViewItem;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            try
            {
                currentFilePath = treeViewItem.Tag.ToString();

                TextEditorOptions options = new TextEditorOptions()
                {
                    IndentationSize = 3,
                    ConvertTabsToSpaces = true,
                    HighlightCurrentLine = true,
                    EnableHyperlinks = true,
                    RequireControlModifierForHyperlinkClick = true,
                    EnableImeSupport = true,
                    CutCopyWholeLine = true
                };

                editor = new TextEditor()
                {
                    MaxHeight = 500,
                    ShowLineNumbers = true,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 15,
                    SyntaxHighlighting = mainWindow.SyntaxHighlighting,
                    Foreground = Brushes.White,
                    LineNumbersForeground = Brushes.White,
                    Options = options
                };

                editor.TextChanged += mainWindow.tbEditor_TextChanged;

                editor.TextArea.CommandBindings.RemoveAt(48); // Delete Line Command 

                editor.Text = File.ReadAllText(currentFilePath);

                TabItem tabItem = new TabItem()
                {
                    Header = Path.GetFileName(currentFilePath),
                    Tag = currentFilePath,
                    Content = editor,
                    Background = Brushes.Gray
                };

                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Tag.ToString() == currentFilePath)
                    {
                        return;
                    }
                }

                tabItem.GotFocus += TabItem_GotFocus;
                tabItem.LostFocus += TabItem_LostFocus;

                tabControl.Items.Add(tabItem);

                tabItem.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void TabItem_LostFocus(object sender, RoutedEventArgs e)
        {
            TabItem tabItem = (TabItem)sender;
            tabItem.Background = Brushes.Red;
        }

        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            TabItem tabItem = (TabItem)sender;
            tabItem.Background = Brushes.DarkGray;
        }

       
    }
}
