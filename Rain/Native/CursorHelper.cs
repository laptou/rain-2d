using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Rain.Native
{
    internal class CursorHelper
    {
        public static IntPtr CreateCursor(Uri resource, float angle)
        {
            var res = Application.GetResourceStream(resource);

            if (res == null) throw new ArgumentException(nameof(resource));

            using (res.Stream)
            {
                var bmp = new Bitmap(res.Stream);
                var height = bmp.Height * 96f / bmp.VerticalResolution;
                var width = bmp.Width * 96f / bmp.HorizontalResolution;
                bmp = new Bitmap(bmp, (int) width, (int) height);
                var ptr = bmp.GetHicon();
                var tmp = new IconInfo();
                GetIconInfo(ptr, ref tmp);
                tmp.fIcon = false;

                return CreateIconIndirect(ref tmp);
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(
            [In] [Optional] IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(
            [In] [Optional] IntPtr hInstance,
            [MarshalAs(UnmanagedType.LPTStr)] string lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor([In] [Optional] IntPtr hCursor);

        #region Nested type: IconInfo

        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public bool   fIcon;
            public int    xHotspot;
            public int    yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        #endregion
    }
}