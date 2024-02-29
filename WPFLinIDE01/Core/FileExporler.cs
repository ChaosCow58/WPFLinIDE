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
        private TextEditor editor;

        public bool openFile = false;
        public string currentFilePath = string.Empty;

        public FileExporler(TreeView treeView, TextEditor textEditor) 
        { 
            this.treeview = treeView;
            editor = textEditor;
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
                    folderNode.ContextMenu = (ContextMenu)treeview.FindResource("ItemContextMenu_Folder");
                    folderNode.Selected += FileNode_Selected;
                    parentNode.Items.Add(folderNode);
                }
                foreach (string file in Directory.GetFiles(path))
                {
                    TreeViewItem fileNode = new TreeViewItem();
                    fileNode.Header = CreateHeader(Path.GetFileName(file), false); // Indicate it's a folder
                    fileNode.Tag = file; // Store full path for later use
                    fileNode.MouseDoubleClick += FileNode_MouseDown;
                    fileNode.ContextMenu = (ContextMenu)treeview.FindResource("ItemContextMenu_File");
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

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
            stack.Background = Brushes.Gray;
        }

        private void FileNode_LostFocus(object sender, RoutedEventArgs e)
        {
            TreeViewItem treeViewItem = (TreeViewItem)sender;

            StackPanel stack = Utility.FindVisualChild<StackPanel>(treeViewItem);
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

        private StackPanel CreateHeader(string text, bool isFolder)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            if (isFolder)
            {
                Image image = new Image();
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/folder.png")); // Set your folder icon path here
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }
            else
            {
                Image image = new Image();
                image.Source = new BitmapImage(new Uri("pack://application:,,,/WPFLinIDE01;component/Assets/file.png"));
                image.Width = 16;
                image.Height = 16;
                panel.Children.Add(image);
            }

            TextBlock headerText = new TextBlock();
            headerText.Foreground = Brushes.White;
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
                            subFolderNode.ContextMenu = (ContextMenu)treeview.FindResource("ItemContextMenu_Folder");
                            subFolderNode.Selected += FileNode_Selected;
                            folderNode.Items.Add(subFolderNode);
                        }

                        foreach (string file in Directory.GetFiles(path))
                        {
                            TreeViewItem fileNode = new TreeViewItem();
                            fileNode.Header = CreateHeader(Path.GetFileName(file), false);
                            fileNode.Tag = file; // Store full path for later use
                            fileNode.MouseDoubleClick += FileNode_MouseDown;
                            fileNode.ContextMenu = (ContextMenu)treeview.FindResource("ItemContextMenu_Folder");
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

        private void FileNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            openFile = true;

            TreeViewItem treeViewItem = sender as TreeViewItem;
            try
            {
                currentFilePath = treeViewItem.Tag.ToString();

                editor.Text = File.ReadAllText(currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
