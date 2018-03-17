using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal static class CaretHelper
    {
        [DllImport("user32.dll")]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int width, int height);

        [DllImport("user32.dll")]
        public static extern bool DestroyCaret();

        [DllImport("user32.dll")]
        public static extern bool GetCaretPos([Out] out NativePoint lpPoint);

        [DllImport("user32.dll")]
        public static extern bool HideCaret([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool ShowCaret([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetCaretBlinkTime();

        [DllImport("user32.dll")]
        public static extern bool SetCaretBlinkTime(uint ms);
    }
}