using System;
using System.Runtime.InteropServices;

namespace Ibinimator.Native {
    internal static class CaretHelper {
        [DllImport("user32.dll")]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int width, int height);

        [DllImport("user32.dll")]
        public static extern bool DestroyCaret();

        [DllImport("user32.dll")]
        public static extern bool GetCaretPos([Out] out NativePoint lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool ShowCaret([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool HideCaret([Optional] IntPtr hWnd);
    }
}