using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Shared
{
    public static class MathUtils
    {
        public static T Clamp<T>(T min, T max, T value) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static T Max<T>(params T[] x) => x.Max();

        public static T Min<T>(params T[] x) => x.Min();

        public const double PI2 = Math.PI * 2;

        public static readonly double SQRT2 = Math.Sqrt(2);
        public static readonly double SQRT3 = Math.Sqrt(3);
        public static readonly double SQRT3_2 = Math.Sqrt(3) / 2;
        public static readonly double SQRT2_2 = Math.Sqrt(2) / 2;
        public static readonly double SQRT1_3 = 1 / Math.Sqrt(3);
        public static readonly double SQRT1_2 = 1 / Math.Sqrt(2);

        public static Vector2 Transform2D(Vector2 v, Matrix3x2 m)
        {
            return v * m.ScaleVector + m.TranslationVector;
        }

        public static RectangleF Transform2D(RectangleF r, Matrix3x2 m)
        {
            return new RectangleF(
                r.X * m.ScaleVector.X + m.TranslationVector.X,
                r.Y * m.ScaleVector.Y + m.TranslationVector.Y,
                r.Width * m.ScaleVector.X,
                r.Height * m.ScaleVector.Y);
        }
    }
}