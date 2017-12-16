using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Native
{
    internal delegate IntPtr WndProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WndClass
    {
        public uint style;
        [MarshalAs(UnmanagedType.FunctionPtr)] public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpszMenuName;

        [MarshalAs(UnmanagedType.LPTStr)] public string lpszClassName;

        
    }
}