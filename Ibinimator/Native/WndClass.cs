using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ibinimator.Native
{
    internal delegate IntPtr WndProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WndClass
    {
        public                                        int     cbClsExtra;
        public                                        int     cbWndExtra;
        public                                        IntPtr  hbrBackground;
        public                                        IntPtr  hCursor;
        public                                        IntPtr  hIcon;
        public                                        IntPtr  hInstance;
        [MarshalAs(UnmanagedType.FunctionPtr)] public WndProc lpfnWndProc;

        [MarshalAs(UnmanagedType.LPTStr)] public string lpszClassName;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpszMenuName;
        public                                   uint   style;
    }
}