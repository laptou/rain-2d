﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;

using Ibinimator.Core.Input;
using Ibinimator.Core.Model;

namespace Ibinimator.Model {}

namespace Ibinimator.Native
{
    internal enum AccentState
    {
        ACCENT_DISABLED                   = 1,
        ACCENT_ENABLE_GRADIENT            = 0,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND          = 3,
        ACCENT_INVALID_STATE              = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int         AccentFlags;
        public int         GradientColor;
        public int         AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr                     Data;
        public int                        SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    internal static class NativeHelper
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hwnd, out PaintStruct lpPaint);

        [DllImport("user32.dll")]
        public static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        public static extern bool DispatchMessage(ref MSG msg);

        [DllImport("user32.dll")]
        public static extern IntPtr ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr UpdateWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitap, int width, int height);

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint                                    dwExStyle,
            ushort                                  lpszClassName,
            string                                  lpszWindowName,
            WindowStyles                            style,
            int                                     x, int     y,
            int                                     width, int height,
            IntPtr                                  hwndParent,
            IntPtr                                  hMenu,
            IntPtr                                  hInst,
            [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(
            IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool DestroyCaret();

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, [In] ref PaintStruct lpPaint);

        [DllImport("user32.dll")]
        public static extern ushort GetAsyncKeyState([In] int vKey);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        public static ModifierState GetModifierState(IntPtr wParam)
        {
            var mod = LowWord(wParam);
            var alt = GetAsyncKeyState(0x12); // VK_MENU (Alt)

            return new ModifierState(
                    (mod & 0x0008) != 0, // MK_CONTROL
                    (mod & 0x0004) != 0, // MK_SHIFT
                    (alt & 0x8000) != 0,
                    (mod & 0x0001) != 0, // MK_LBUTTON
                    (mod & 0x0010) != 0, // MK_MBUTTON
                    (mod & 0x0002) != 0, // MK_RBUTTON
                    (mod & 0x0020) != 0, // MK_XBUTTON1
                    (mod & 0x0040) != 0 // MK_XBUTTON2
                );
        }

        public static ModifierState GetModifierState()
        {
            var shift = GetAsyncKeyState(0x10); // VK_SHIFT
            var ctrl = GetAsyncKeyState(0x11); // VK_CONTROL
            var alt = GetAsyncKeyState(0x12); // VK_MENU (Alt)

            var lmb = GetAsyncKeyState(0x01); // VK_LBUTTON
            var rmb = GetAsyncKeyState(0x02); // VK_RBUTTON
            var mmb = GetAsyncKeyState(0x04); // VK_MBUTTON
            var xmb = GetAsyncKeyState(0x05); // VK_XBUTTON1
            var ymb = GetAsyncKeyState(0x06); // VK_XBUTTON2

            return new ModifierState(
                    (ctrl & 0x8000) != 0,
                    (shift & 0x8000) != 0,
                    (alt & 0x8000) != 0,
                    (lmb & 0x8000) != 0,
                    (mmb & 0x8000) != 0,
                    (rmb & 0x8000) != 0,
                    (xmb & 0x8000) != 0,
                    (ymb & 0x8000) != 0
                );
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(
            [In] [Optional] [MarshalAs(UnmanagedType.LPTStr)]
            string lpModuleName);

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

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(
            [In] IntPtr hWnd,
            [In] IntPtr lpRect,
            [In] bool   bErase);

        [DllImport("user32.dll")]
        public static extern bool IsWindowUnicode(IntPtr hWnd);

        /// <summary>
        ///     Gets low bits values of the pointer.
        /// </summary>
        public static short LowWord(IntPtr ptr)
        {
            var val32 = (ulong) ptr;

            return (short) (val32 & 0x0000FFFF);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ushort RegisterClass(ref WndClass lpWndClass);


        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus([Optional] IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern ushort UnregisterClass(
            [MarshalAs(UnmanagedType.LPTStr)] string lpClassName,
            [Optional]                        IntPtr hInstance);

        [DllImport("user32.dll")]
        public static extern bool ValidateRect(
            [In]     IntPtr     hWnd,
            [In] ref NativeRect lpRect);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect
    {
        public int Left, Top, Right, Bottom;

        public NativeRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public NativeRect(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

        public int Height
        {
            get => Bottom - Top;
            set => Bottom = value + Top;
        }

        public Point Location
        {
            get => new Point(Left, Top);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Size Size
        {
            get => new Size(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public int Width
        {
            get => Right - Left;
            set => Right = value + Left;
        }

        public int X
        {
            get => Left;
            set
            {
                Right -= Left - value;
                Left = value;
            }
        }

        public int Y
        {
            get => Top;
            set
            {
                Bottom -= Top - value;
                Top = value;
            }
        }

        public bool Equals(NativeRect r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is NativeRect rect)
                return Equals(rect);
            if (obj is Rectangle rectangle)
                return Equals(new NativeRect(rectangle));

            return false;
        }

        public override int GetHashCode() { return ((Rectangle) this).GetHashCode(); }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }

        public static bool operator ==(NativeRect r1, NativeRect r2) { return r1.Equals(r2); }

        public static implicit operator Rectangle(NativeRect r)
        {
            return new Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator NativeRect(Rectangle r) { return new NativeRect(r); }

        public static bool operator !=(NativeRect r1, NativeRect r2) { return !r1.Equals(r2); }
    }
}