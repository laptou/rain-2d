using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Renderer.Direct2D
{
    internal class NativeHelper
    {
        [DllImport("user32.dll")]
        public static extern bool HideCaret([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool ShowCaret([Optional] IntPtr hWnd);
    }
}