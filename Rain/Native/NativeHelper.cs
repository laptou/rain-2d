using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal static class NativeHelper
    {
        public static void CheckError()
        {
            var error = Marshal.GetLastWin32Error();

            if (error != 0) throw new Win32Exception(error);
        }

        public static Vector2 GetCoordinates(IntPtr lParam, float dpi)
        {
            var x = LowWord(lParam) / dpi * 96f;
            var y = HighWord(lParam) / dpi * 96f;

            return new Vector2(x, y);
        }

        public static Vector2 GetCoordinates(IntPtr lParam, float dpi, IntPtr hWnd)
        {
            var pt = new NativePoint {x = LowWord(lParam), y = HighWord(lParam)};
            WindowHelper.ScreenToClient(hWnd, ref pt);
            var x = pt.x / dpi * 96f;
            var y = pt.y / dpi * 96f;

            return new Vector2(x, y);
        }

        /// <summary>
        ///     Gets high bits values of the pointer.
        /// </summary>
        public static short HighWord(IntPtr ptr)
        {
            unchecked
            {
                var val32 = (ulong) ptr;

                return (short) ((val32 & 0xFFFF0000) >> 16);
            }
        }

        /// <summary>
        ///     Gets low bits values of the pointer.
        /// </summary>
        public static short LowWord(IntPtr ptr)
        {
            var val32 = (ulong) ptr;

            return (short) (val32 & 0x0000FFFF);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WaitForSingleObjectEx(
            [In] IntPtr hHandle, [In] uint dwMilliseconds, [In] bool bAlertable);
    }
}