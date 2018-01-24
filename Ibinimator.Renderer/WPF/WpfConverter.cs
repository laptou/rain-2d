using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Ibinimator.Core.Model;

using Color = Ibinimator.Core.Model.Color;

namespace Ibinimator.Renderer.WPF
{
    public static class WpfConverter
    {
        public static Color Convert(this System.Windows.Media.Color c)
        {
            return new Color(
                c.R / 255f,
                c.G / 255f,
                c.B / 255f,
                c.A / 255f);
        }

        public static System.Windows.Media.Color Convert(this Color c)
        {
            return System.Windows.Media.Color.FromArgb(
                (byte) (c.Alpha * 255),
                (byte) (c.Red * 255),
                (byte) (c.Green * 255),
                (byte) (c.Blue * 255));
        }

        public static Point Convert(this Vector2 vec) { return new Point(vec.X, vec.Y); }

        public static Vector2 Convert(this Point vec) { return new Vector2((float) vec.X, (float) vec.Y); }

        public static RectangleF Convert(this Rect rect)
        {
            return new RectangleF(
                (float) rect.Left,
                (float) rect.Top,
                (float) rect.Width,
                (float) rect.Height);
        }

        public static Matrix Convert(this Matrix3x2 mat)
        {
            return new Matrix(mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32);
        }

        public static Matrix3x2 Convert(this Matrix mat)
        {
            return new Matrix3x2(
                (float) mat.M11,
                (float) mat.M12,
                (float) mat.M21,
                (float) mat.M22,
                (float) mat.OffsetX,
                (float) mat.OffsetY);
        }
    }
}