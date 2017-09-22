using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Mathematics
{
    public static class MathUtils
    {
        public const float TwoPi = (float) Math.PI * 2;
        public const float PiOverTwo = (float) Math.PI / 2;
        public const float Epsilon = 1e-7f;

        public static readonly float Sqrt2 = (float) Math.Sqrt(2);
        public static readonly float Sqrt3 = (float) Math.Sqrt(3);
        public static readonly float Sqrt3Over2 = (float) Math.Sqrt(3) / 2;
        public static readonly float Sqrt2Over2 = (float) Math.Sqrt(2) / 2;
        public static readonly float InverseSqrt3 = 1 / (float) Math.Sqrt(3);
        public static readonly float InverseSqrt2 = 1 / (float) Math.Sqrt(2);

        public static Vector2 Abs(Vector2 v)
        {
            return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
        }

        public static double AbsMax(double min, double x)
        {
            return Math.Max(min, Math.Abs(x)) * (x < 0 ? -1 : 1);
        }

        public static float AbsMax(float min, float x)
        {
            return Math.Max(min, Math.Abs(x)) * (x < 0 ? -1 : 1);
        }

        public static float AbsSqrt(float f)
        {
            return (float) Math.Sqrt(Math.Abs(f)) * Math.Sign(f);
        }

        public static RectF Bounds(RectF rect, Matrix m)
        {
            Vector2 p0 = m * rect.TopLeft,
                p1 = m * rect.TopRight,
                p2 = m * rect.BottomRight,
                p3 = m * rect.BottomLeft;

            float l = Min(p0.X, p1.X, p2.X, p3.X),
                t = Min(p0.Y, p1.Y, p2.Y, p3.Y),
                r = Max(p0.X, p1.X, p2.X, p3.X),
                b = Max(p0.Y, p1.Y, p2.Y, p3.Y);

            return new RectF(t, l, r, b);
        }

        public static T Clamp<T>(T min, T max, T value) where T : IComparable
        {
            return value.CompareTo(min) < 0 ? min : (value.CompareTo(max) > 0 ? max : value);
        }

        public static bool ContainsNaN(Matrix m)
        {
            return Enumerable.Range(0, 6).Any(i => float.IsNaN(m[i]));
        }

        public static (Vector2, Vector2) CrossSection(Vector2 ray, Vector2 origin, RectF bounds)
        {
            bounds = bounds.Offset(-origin);

            Vector2 v1;
            Vector2 v2;
            var r = Abs(ray);
            var o = Abs(bounds.TopRight);
            var angle = Math.Atan2(-r.Y, r.X);
            var tan = (float) Math.Tan(angle);

            if (Math.Abs(angle) > Math.Abs(Math.Atan2(-o.Y, o.X)))
            {
                v1 = new Vector2(bounds.Bottom / tan, bounds.Bottom);
                v2 = new Vector2(bounds.Top / tan, bounds.Top);
            }
            else
            {
                v1 = new Vector2(bounds.Left, bounds.Left * tan);
                v2 = new Vector2(bounds.Right, bounds.Right * tan);
            }

            return (v2, v1);
        }

        public static RectangleF Inflate(this RectangleF rect, float amount)
        {
            var r = rect; // this should copy by value, since RectangleF is struct
            r.Inflate(amount, amount);
            return r;
        }

        public static T Max<T>(params T[] x)
        {
            return x.Max();
        }

        public static T Min<T>(params T[] x)
        {
            return x.Min();
        }

        public static Vector2 Project(Vector2 a, Vector2 b)
        {
            return b * Vector2.Dot(a, b) / Vector2.Dot(b, b);
        }

        public static Vector2 Rotate(Vector2 v, float theta)
        {
            var cs = (float) Math.Cos(theta);
            var sn = (float) Math.Sin(theta);

            var px = v.X * cs - v.Y * sn;
            var py = v.X * sn + v.Y * cs;

            return new Vector2(px, py);
        }

        public static Vector2 Rotate(Vector2 v, Vector2 c, float theta)
        {
            return Rotate(v - c, theta) + c;
        }

        public static RectF Round(this RectF rect)
        {
            return new RectF((int) Math.Round(rect.Left), (int) Math.Round(rect.Top),
                (int) Math.Round(rect.Right), (int) Math.Round(rect.Bottom));
        }

        public static Vector2 Scale(Vector2 v, Vector2 c, Vector2 s)
        {
            return (v - c) * s + c;
        }

        public static Vector2 ShearX(Vector2 v, float theta)
        {
            return new Vector2(v.X + (float) Math.Tan(theta) * v.Y, v.Y);
        }

        public static Vector2 ShearX(Vector2 v, Vector2 o, float theta)
        {
            return ShearX(v - o, theta) + o;
        }

        public static Vector2 Sign(Vector2 v)
        {
            return new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
        }

        public static Vector2 Sqrt(Vector2 v)
        {
            return new Vector2(AbsSqrt(v.X), AbsSqrt(v.Y));
        }

        public static Vector2 UnitVector(float angle)
        {
            return new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle));
        }

        public static float Wrap(float f, float r)
        {
            return (f % r + r) % r;
        }
    }
}