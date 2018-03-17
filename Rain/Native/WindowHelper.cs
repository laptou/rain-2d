using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.UnmanagedType;

namespace Rain.Native
{
    internal static class DragHelper
    {
        [DllImport("shell32.dll")]
        public static extern bool DragQueryPoint(IntPtr hDrop, out NativePoint lppt);

        [DllImport("shell32.dll")]
        public static extern void DragFinish();

        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [DllImport("shell32.dll")]
        public static extern int DragQueryFile(
            [In] IntPtr hDrop, [In] uint iFile, [Out] StringBuilder lpszFile, int cch);
    }

    internal static class WindowHelper
    {
        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hWnd, out PaintStruct lpPaint);

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            WindowStylesEx dwExStyle, ushort lpszClassName, string lpszWindowName,
            WindowStyles style, int x, int y, int width, int height, IntPtr hWndParent,
            IntPtr hMenu, IntPtr hInst, [MarshalAs(AsAny)] object pvParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(
            IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, [In] ref PaintStruct lpPaint);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(
            [In] IntPtr hWnd, [In] IntPtr lpRect, [In] bool bErase);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(
            [In] IntPtr hWnd, [In] ref NativeRect lpRect, [In] bool bErase);

        [DllImport("user32.dll")]
        public static extern bool IsWindowUnicode(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr RedrawWindow(
            IntPtr hWnd, [In] ref NativeRect lpRect, IntPtr hRgn, uint flags);

        [DllImport("user32.dll")]
        public static extern IntPtr RedrawWindow(
            IntPtr hWnd, IntPtr lpRect, IntPtr hRgn, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClass(ref WndClass lpWndClass);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref NativePoint lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(
            IntPtr hWnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        public static extern ushort UnregisterClass(
            [MarshalAs(LPTStr)] string lpClassName, [Optional] IntPtr hInstance);

        [DllImport("user32.dll")]
        public static extern IntPtr UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ValidateRect([In] IntPtr hWnd, [In] ref NativeRect lpRect);

        [DllImport("user32.dll")]
        public static extern bool ValidateRect([In] IntPtr hWnd, [In] IntPtr lpRect);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int code);
    }
}