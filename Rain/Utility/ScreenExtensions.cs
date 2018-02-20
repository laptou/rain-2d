using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using Point = System.Drawing.Point;

namespace Rain.Utility
{
    public static class ScreenExtensions
    {
        public static (uint x, uint y) GetDpiForMonitor(this Screen screen, DpiType type)
        {
            var pnt = new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2 /*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, type, out var x, out var y);

            return (x, y);
        }

        public static (uint x, uint y) GetDpiForWindow(
            this Screen screen, DpiType type, Window window)
        {
            var wih = new WindowInteropHelper(window);

            var mon = MonitorFromWindow(wih.Handle, 2 /*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, type, out var x, out var y);

            return (x, y);
        }

        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor(
            [In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromWindow([In] IntPtr hwnd, [In] uint dwFlags);
    }

    public enum DpiType
    {
        Effective = 0,
        Angular   = 1,
        Raw       = 2
    }
}