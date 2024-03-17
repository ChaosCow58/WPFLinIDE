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

namespace WPFLinIDE01.Core
{
    public enum ItemType
    {
        Folder,
        File
    };

    public class ExplorlerTreeViewItem : TreeViewItem
    {
        public static readonly DependencyProperty ItemTypeProperty =
            DependencyProperty.Register("ItemType", typeof(ItemType), typeof(ExplorlerTreeViewItem));

        public ItemType ItemType 
        {
            get { return (ItemType)GetValue(ItemTypeProperty);  }
            set { SetValue(ItemTypeProperty, value); } 
        }

        public ExplorlerTreeViewItem() { }
    }

    public class FileExporler
    {
        public TreeView treeview;

        public TextEditor editor;
        public TabControl tabControl;

        public bool openFile = false;
        public string currentFilePath = string.Empty;

      

        public FileExporler(TreeView treeView, TabControl tabControl)
        {
            this.treeview = treeView;
            this.tabControl = tabControl;
        }

        private void PopulateTreeView(string path, ExplorlerTreeViewItem parentNode)
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(path))
                {
                    if (Path.GetFileName(folder) == "bin" || Path.GetFileName(folder) == "obj")
                    {
                        continue;
                    }

                    ExplorlerTreeViewItem folderNode = new ExplorlerTreeViewItem();
                    folderNode.Header = CreateHeader(Path.GetFileName(folder), true); // Indicate it's a folder
                    folderNode.Tag = folder; // Store full path for later use
                    folderNode.Items.Add("*"); // Placeholder to show expand/collapse arrow
                    folderNode.Expanded += Folder_Expanded; // Attach event for lazy loading
                    folderNode.ItemType = ItemType.Folder;
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    ExplorlerTreeViewItem fileNode = new ExplorlerTreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
                    fileNode.LostFocus += FileNode_LostFocus;
                    fileNode.GotFocus += FileNode_GotFocus;
                    fileNode.ItemType = ItemType.File;
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
            ExplorlerTreeViewItem treeViewItem = (ExplorlerTreeViewItem)sender;

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
            if (stack != null)
            { 
                stack.Background = Brushes.Gray;
            }
        }

        private void FileNode_LostFocus(object sender, RoutedEventArgs e)
        {
            ExplorlerTreeViewItem treeViewItem = (ExplorlerTreeViewItem)sender;

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
            if (stack != null)
            {
                stack.Background = Brushes.DarkGray;
            }
        }

        public void DisplayFileSystem()
        {
            string rootFolder = MetaDataFile.GetMetaValue<string>("ProjectPath");

            ExplorlerTreeViewItem rootNode = new ExplorlerTreeViewItem();
            rootNode.Header = CreateHeader(MetaDataFile.GetMetaValue<string>("ProjectName"), true); // Indicate it's a folder
            rootNode.Tag = rootFolder;
            treeview.Items.Add(rootNode);

            PopulateTreeView(rootFolder, rootNode);

            // Expand the root node
            rootNode.IsExpanded = true;
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
                        Command = new RelayCommand(ItemRenameMenuItem_Click)
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
            ItemRenameMenuItemBase();
        }

        public void ItemRenameMenuItemBase()
        {
            ExplorlerTreeViewItem treeViewItem = (ExplorlerTreeViewItem)treeview.SelectedItem;

            if (treeViewItem != null)
            {
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
                            if (treeViewItem.ItemType == ItemType.Folder)
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

                                        goto updateUI;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Error renaming directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    goto updateUI;
                                }
                            }
                            else if (treeViewItem.ItemType == ItemType.File)
                            {
                                string oldFile = treeViewItem.Tag.ToString();
                                string newFile = @$"{Path.GetDirectoryName(treeViewItem.Tag.ToString())}\{textBox.Text}";

                                try
                                {
                                    if (File.Exists(treeViewItem.Tag.ToString()))
                                    {
                                        File.Move(oldFile, newFile);
                                        treeViewItem.Tag = newFile;

                                        TabItem selectedItem = (TabItem)tabControl.SelectedItem;

                                        // Rename method
                                        foreach (TabItem tabItem in tabControl.Items)
                                        {
                                            if (tabItem.Tag.ToString() == oldFile)
                                            {
                                                tabItem.Header = Path.GetFileName(newFile);
                                                tabItem.Tag = newFile;
                                                break; // Exit the loop since the tab has been renamed
                                            }
                                        }

                                        // Check for duplicates and open tabs
                                        foreach (TabItem tabItem in tabControl.Items)
                                        {
                                            if (tabItem.Tag.ToString() == newFile && tabItem != selectedItem)
                                            {
                                                // Close duplicate tab
                                                tabControl.Items.Remove(tabItem);
                                                break; // Exit the loop since a duplicate has been removed
                                            }
                                        }



                                        goto updateUI;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Error renaming directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    goto updateUI;
                                }
                            }

                        updateUI:
                            // Update UI elements
                            textBox.IsReadOnly = true;
                            textBox.Foreground = Brushes.White;
                            textBox.Background = Brushes.Transparent;
                            textBox.IsHitTestVisible = false;
                        }
                    };
                }
            }
        }

        private void ItemAddMenuItem_Click(object parameter) 
        {
            ExplorlerTreeViewItem selectedItem = (ExplorlerTreeViewItem)treeview.SelectedItem;

            if (selectedItem != null)
            { 
                string fullPath = selectedItem.Tag.ToString();
                string convertedPath = ConvertToRelativePath(fullPath);

                App.Current.Properties["DotPath"] = convertedPath.Replace('/', '.');


                NameFileWindow nameFileWindow = new NameFileWindow();
                nameFileWindow.ShowDialog();

                if (!string.IsNullOrEmpty(App.Current.Properties["FileName"]?.ToString()))
                {
                    using (StreamWriter sm = new StreamWriter(@$"{fullPath}\{App.Current.Properties["FileName"]}.cs"))
                    {
                        if (File.Exists(@$"{fullPath}\{App.Current.Properties["FileName"]}.cs"))
                        {
                            MessageBoxResult messageBoxResult = MessageBox.Show("This file already exists do you want to replace it?", "Replace File", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                            if (messageBoxResult == MessageBoxResult.Yes)
                            {
                                sm.WriteLine($"using System;\n\nnamespace {App.Current.Properties["DotPath"]}\n{{\n\tclass {App.Current.Properties["FileName"]}\n\t{{\n\n\t}}\n}}");
                                InitializeFileSystemWatcher(@$"{fullPath}\{App.Current.Properties["FileName"]}.cs");
                                sm.Close();
                            }
                            else
                            {
                                sm.Close();
                            }
                        }
                    }

                    App.Current.Properties["FileName"] = null;

                    treeview.Items.Clear();
                    DisplayFileSystem();
                    


                }
            }
        }

        private string ConvertToRelativePath(string fullPath)
        {
            int projectNameIndex = fullPath.IndexOf(MetaDataFile.GetMetaValue<string>("ProjectName"));

            string rootDirectory = string.Empty; // Root directory path

            if (projectNameIndex >= 0)
            {
                rootDirectory = fullPath.Substring(0, projectNameIndex);
            }


            // Check if the full path starts with the root directory
            if (fullPath.StartsWith(rootDirectory))
            {
                // Remove the root directory path
                string relativePath = fullPath.Substring(rootDirectory.Length);

                // Replace backslashes with forward slashes
                relativePath = relativePath.Replace("\\", "/");

                return relativePath;
            }
            else
            {
                // If the full path doesn't start with the root directory,
                // return the full path as is
                return fullPath;
            }
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            ExplorlerTreeViewItem folderNode = (ExplorlerTreeViewItem)sender;
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
                            ExplorlerTreeViewItem subFolderNode = new ExplorlerTreeViewItem();
                            subFolderNode.Header = CreateHeader(Path.GetFileName(folder), true);
                            subFolderNode.Tag = folder;
                            subFolderNode.Items.Add("*"); // Placeholder for sub-nodes
                            subFolderNode.Expanded += Folder_Expanded; // Attach event for lazy loading
                            subFolderNode.ItemType = ItemType.Folder;
                            folderNode.Items.Add(subFolderNode);
                        }

                        foreach (string file in Directory.GetFiles(path))
                        {
                            ExplorlerTreeViewItem fileNode = new ExplorlerTreeViewItem();
                            fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                            fileNode.Tag = file; // Store full path for later use
                            fileNode.MouseDoubleClick += FileNode_MouseDown;
                            fileNode.LostFocus += FileNode_LostFocus;
                            fileNode.GotFocus += FileNode_GotFocus;
                            fileNode.ItemType = ItemType.File;
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

            ExplorlerTreeViewItem treeViewItem = (ExplorlerTreeViewItem)sender;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            try
            {
                currentFilePath = treeViewItem.Tag.ToString();

                TextEditorOptions options = new TextEditorOptions()
                {
                    IndentationSize                         = MetaDataFile.GetMetaValue<int>("EditorSettings.IndentationSize"),
                    ConvertTabsToSpaces                     = MetaDataFile.GetMetaValue<bool>("EditorSettings.ConvertTabsToSpaces"),
                    HighlightCurrentLine                    = MetaDataFile.GetMetaValue<bool>("EditorSettings.HighlightCurrentLine"),
                    EnableHyperlinks                        = MetaDataFile.GetMetaValue<bool>("EditorSettings.EnableHyperlinks"),
                    RequireControlModifierForHyperlinkClick = MetaDataFile.GetMetaValue<bool>("EditorSettings.RequireControlModifierForHyperlinkClick"),
                    EnableImeSupport                        = MetaDataFile.GetMetaValue<bool>("EditorSettings.EnableImeSupport"),
                    CutCopyWholeLine                        = MetaDataFile.GetMetaValue<bool>("EditorSettings.CutCopyWholeLine")
                };

                editor = new TextEditor()
                {
                    MaxHeight = 500,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    SyntaxHighlighting = mainWindow.SyntaxHighlighting,

                    ShowLineNumbers = MetaDataFile.GetMetaValue<bool>("EditorSettings.ShowLineNumbers"),
                    FontSize = MetaDataFile.GetMetaValue<double>("EditorSettings.FontSize"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MetaDataFile.GetMetaValue<string>("EditorSettings.Foreground"))),
                    LineNumbersForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MetaDataFile.GetMetaValue<string>("EditorSettings.LineNumbersForeground"))),
                    
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

        // Declare a file system watcher at class level
        private FileSystemWatcher fileWatcher;

        // Initialize the file system watcher
        private void InitializeFileSystemWatcher(string filePath)
        {
            // Ensure to unsubscribe previous events
            if (fileWatcher != null)
            {
                fileWatcher.Changed -= FileWatcher_Changed;
                fileWatcher.Dispose();
            }

            fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
            fileWatcher.Filter = Path.GetFileName(filePath);
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.EnableRaisingEvents = true;
        }

        // Event handler for file changes
        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        editor.Text = File.ReadAllText(e.FullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

            }
        }



    } // Class
} // Namespace
