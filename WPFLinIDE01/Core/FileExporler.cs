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
                    folderNode.Selected += FileNode_Selected;
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
                    fileNode.Selected += FileNode_Selected;
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

            MenuItem stack = Utility.FindVisualChild<MenuItem>(treeViewItem);
            stack.Background = Brushes.Gray;
        }

        private void FileNode_LostFocus(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)sender;

            MenuItem stack = Utility.FindVisualChild<MenuItem>(treeViewItem);
            stack.Background = Brushes.DarkGray;
        }

        public void DisplayFileSystem()
        {
            string rootFolder = App.Current.Properties["ProjectPath"].ToString();

            TreeViewItem rootNode = new TreeViewItem();
            rootNode.Header = CreateHeader(App.Current.Properties["ProjectName"].ToString(), true); // Indicate it's a folder
            treeview.Items.Add(rootNode);

            PopulateTreeView(rootFolder, rootNode);
        }

        private Menu CreateHeader(string text, bool isFolder)
        {
            MainWindow window = App.Current.MainWindow as MainWindow;

            Menu menu = new Menu() 
            { 
                Background = Brushes.Transparent,
            };


            if (isFolder)
            {
                Image image = new Image();
                MenuItem miFolder = new MenuItem()
                {
                    Name = "miFolder",
                    Header = "Folder",

                    Items = {
                        new MenuItem() 
                        {
                             Header = "Add",
                             Foreground = Brushes.Black
                        },

                        new MenuItem()
                        {
                            Header = "Rename",
                            InputGestureText = "F2",
                            Foreground = Brushes.Black
                        },

                        new MenuItem() 
                        {
                            Header = "Delete",
                            InputGestureText = "Del",
                            Foreground = Brushes.Black,
                           
                        }
                    }
                };

                miFolder.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(ItemRenameMenuItem_Click));
                miFolder.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(ItemRenameMenuItem_Click));
                miFolder.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(ItemDeleteMenuItem_Click));



                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/folder.png")); // Set your folder icon path here
                image.Width = 16;
                image.Height = 16;
                miFolder.Icon = image;

                miFolder.Header = text;
                miFolder.Foreground = Brushes.White;
                menu.Items.Add(miFolder);

            }
            else
            {
                Image image = new Image();
                MenuItem menuItem2 = (MenuItem)window.FindResource("ItemContextMenu_File");
                

                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/file.png"));
                image.Width = 16;
                image.Height = 16;
                menuItem2.Icon = image;

                menuItem2.Header = text;
                menuItem2.Foreground = Brushes.White;
                menu.Items.Add(menuItem2);

            }
            //menuItem.Header = text;
            //menuItem.Foreground = Brushes.White;
            return menu;
        }

        private void ItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Delete");
        }

        private void ItemRenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Rename");
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
                            subFolderNode.Selected += FileNode_Selected;
                            folderNode.Items.Add(subFolderNode);
                        }

                        foreach (string file in Directory.GetFiles(path))
                        {
                            TreeViewItem fileNode = new TreeViewItem();
                            fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                            fileNode.Tag = file; // Store full path for later use
                            fileNode.MouseDoubleClick += FileNode_MouseDown;
                            fileNode.Selected += FileNode_Selected;
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

        private void FileNode_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)treeview.SelectedItem;

            if (treeViewItem != null)
            {
                if (Path.HasExtension(Path.GetFileName(treeViewItem.Tag.ToString())))
                {
                    if (Mouse.RightButton == MouseButtonState.Pressed)
                    {
                        ContextMenu context = (ContextMenu)treeview.FindResource("ItemContextMenu_File");
                        if (context != null)
                        {
                            context.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    ContextMenu context = (ContextMenu)treeview.FindResource("ItemContextMenu_Folder");
                    if (context != null)
                    {
                        context.Visibility = Visibility.Visible;
                    }
                }
            }
            e.Handled = true;
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
                tabItem.GotFocus += TabItem_GotFocus;
                tabItem.LostFocus += TabItem_LostFocus;

                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Tag.ToString() == currentFilePath)
                    {
                        return;
                    }
                }

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
