using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int x;
        public int y;
    }
}