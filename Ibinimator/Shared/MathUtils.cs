using SharpDX;
using SharpDX.Mathematics.Interop;
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

        public static double AbsMax(double min, double x) =>
            Math.Max(min, Math.Abs(x)) * (x < 0 ? -1 : 1);

        public static float AbsMax(float min, float x) =>
            Math.Max(min, Math.Abs(x)) * (x < 0 ? -1 : 1);

        public static float AbsSqrt(float f) => (float)Math.Sqrt(Math.Abs(f)) * Math.Sign(f);

        public static RectangleF Inflate(this RectangleF rect, float amount)
        {
            RectangleF r = rect; // this should copy by value, since RectangleF is struct
            r.Inflate(amount, amount);
            return r;
        }

        public static Rectangle Round(this RectangleF rect) =>
            new Rectangle((int)Math.Round(rect.X), (int)Math.Round(rect.Y), 
                (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));

        public const double PI2 = Math.PI * 2;
        public const double PI_2 = Math.PI / 2;

        public static readonly double SQRT2 = Math.Sqrt(2);
        public static readonly double SQRT3 = Math.Sqrt(3);
        public static readonly double SQRT3_2 = Math.Sqrt(3) / 2;
        public static readonly double SQRT2_2 = Math.Sqrt(2) / 2;
        public static readonly double SQRT1_3 = 1 / Math.Sqrt(3);
        public static readonly double SQRT1_2 = 1 / Math.Sqrt(2);

        public static Vector2 Transform2D(Vector2 v, Matrix3x2 m)
        {
            return Matrix3x2.TransformPoint(m, v);
        }

        public static RectangleF Bounds(RectangleF rect, Matrix3x2 m)
        {
            Vector2 p0 = Matrix3x2.TransformPoint(m, rect.TopLeft),
                p1 = Matrix3x2.TransformPoint(m, rect.TopRight),
                p2 = Matrix3x2.TransformPoint(m, rect.BottomRight),
                p3 = Matrix3x2.TransformPoint(m, rect.BottomLeft);

            float l = Min(p0.X, p1.X, p2.X, p3.X),
                t = Min(p0.Y, p1.Y, p2.Y, p3.Y),
                r = Max(p0.X, p1.X, p2.X, p3.X),
                b = Max(p0.Y, p1.Y, p2.Y, p3.Y);

            return new RectangleF(l, t, r - l, b - t);
        }

        public static RectangleF Convert(this RawRectangleF raw)
        {
            return new RectangleF(raw.Left, raw.Top, raw.Right - raw.Left, raw.Bottom - raw.Top);
        }

        public static (Vector2 scale, float rotation, Vector2 translation, float skew) Decompose(this Matrix3x2 m)
        {
            Vector2 scale = new Vector2(m.Row1.Length(), m.Row2.Length());
            float rotation = (float)Math.Atan2(m.M12, m.M11);
            Vector2 translation = m.Row3;
            float skew = (float)(Math.Acos(m.M11 * m.M21 + m.M12 * m.M22) - PI_2);

            return (scale, rotation, translation, skew);
        }

        public static float GetRotation(this Matrix3x2 m) => (float)Math.Atan2(m.M12, m.M11);

        public static Vector2 GetScale(this Matrix3x2 m) => new Vector2(m.Row1.Length(), m.Row2.Length());

        public static Vector2 Rotate(Vector2 v, float theta)
        {
            var cs = (float)Math.Cos(theta);
            var sn = (float)Math.Sin(theta);

            var px = v.X * cs - v.Y * sn;
            var py = v.X * sn + v.Y * cs;

            return new Vector2(px, py);
        }

        public static Vector2 Rotate(Vector2 v, Vector2 c, float theta) => Rotate(v - c, theta) + c;

        public static Vector2 Scale(Vector2 v, Vector2 c, Vector2 s) => (v - c) * s + c;

        public static Vector2 UnitVector(float angle) => new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

        public static Vector2 Sqrt(Vector2 v) => new Vector2(AbsSqrt(v.X), AbsSqrt(v.Y));

        public static Vector2 Abs(Vector2 v) => new Vector2(Math.Abs(v.X), Math.Abs(v.Y));

        public static Vector2 Sign(Vector2 v) => new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
    }
}