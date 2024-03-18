using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Threading;

using ConsoleControls = ConsoleControl.ConsoleControl;

namespace WPFLinIDE01.Core
{
   
    public class Terminal
    {
        public Process process;
        public ConsoleControls terminal;

        private WindowsFormsHost host;
        private Grid terminalGrid;

        public Terminal(Grid displayGrid) 
        { 
            terminalGrid = displayGrid;
        }

        public void CreateTermial()
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
            // terminal.OnConsoleInput += Terminal_OnConsoleInput;

            terminal.ProcessInterface.WriteInput($"cd '{MetaDataFile.GetMetaValue<string>("ProjectPath")}'");

            terminal.IsInputEnabled = true;
            terminal.AutoScroll = true;
            terminal.Font = new Font("Poppins", 11);
            terminal.BorderStyle = BorderStyle.FixedSingle;

            host = new WindowsFormsHost();
            host.Child = terminal;

            terminalGrid.Children.Add(host);
        }

        private void Terminal_OnConsoleInput(object sender, ConsoleControl.ConsoleEventArgs args)
        {
            if (args.Content == "exit")
            {
               
                terminalGrid.Children.Remove(host);
                terminalGrid.Visibility = Visibility.Collapsed;
               

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
    }
}
