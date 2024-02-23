using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace WPFLinIDE01
{
    public partial class MainWindow : Window
    {

        private Process process;
        private WindowsFormsHost host;
        private ConsoleControl.ConsoleControl terminal;

        public ICommand Show_PowerShell { get; }

        public MainWindow()
        {
            InitializeComponent();

            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            Show_PowerShell = new RelayCommand(ShowPowerShell);

            DataContext = this;
        }

        private void ShowPowerShell(object parameter)
        {
            if (gTermialPanel.Visibility == Visibility.Collapsed)
            {
                CreateTermial();
                gTermialPanel.Visibility = Visibility.Visible;
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
            CreateTermial();
        }

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

            terminal = new ConsoleControl.ConsoleControl();

            process.Start();

            terminal.ProcessInterface.StartProcess(process.StartInfo);
            terminal.OnConsoleInput += Terminal_OnConsoleInput;


            terminal.IsInputEnabled = true;
            terminal.Font = new Font("Poppins", 11);


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


    }
}
