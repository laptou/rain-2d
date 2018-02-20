using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PaintStruct
    {
        public bool       fErase;
        public bool       fIncUpdate;
        public bool       fRestore;
        public IntPtr     hdc;
        public NativeRect rcPaint;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }
}