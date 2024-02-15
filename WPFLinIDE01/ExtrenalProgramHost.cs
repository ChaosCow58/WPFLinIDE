using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace WPFLinIDE01
{
    internal class ExtrenalProgramHost : HwndHost
    {
        private IntPtr _hwnd;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            Process externalProcess = new Process();
            externalProcess.StartInfo.FileName = "powershell.exe";
            externalProcess.Start();
            externalProcess.WaitForInputIdle();

            _hwnd = externalProcess.MainWindowHandle;

            if (_hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get the handle of the external process window.");
            }

            if (!NativeMethods.IsWindowVisible(_hwnd))
            {
                throw new InvalidOperationException("The external process window is not visible.");
            }

            NativeMethods.SetParent(_hwnd, hwndParent.Handle);
            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            NativeMethods.SetParent(_hwnd, IntPtr.Zero);
            _hwnd = IntPtr.Zero;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        private class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern bool IsWindowVisible(IntPtr hWnd);
        }
    }
}
