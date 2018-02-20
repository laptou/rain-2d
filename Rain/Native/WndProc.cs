using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal delegate IntPtr WndProc(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);
}