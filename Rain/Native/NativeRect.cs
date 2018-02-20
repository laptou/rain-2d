using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
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
                                 "{{Left={0},Top={1},Right={2},Bottom={3}}}",
                                 Left,
                                 Top,
                                 Right,
                                 Bottom);
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