using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Utility
{
    public static class MathUtils
    {
        public const float TwoPi = (float) Math.PI * 2;
        public const float PiOverTwo = (float) Math.PI / 2;

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

        public static T Clamp<T>(T min, T max, T value) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static bool ContainsNaN(Matrix3x2 m)
        {
            return new[] {0, 1, 2, 3, 4, 5}.Any(i => float.IsNaN(m[i]));
        }

        public static RectangleF Convert(this RawRectangleF raw)
        {
            return new RectangleF(raw.Left, raw.Top, raw.Right - raw.Left, raw.Bottom - raw.Top);
        }

        public static Vector2 Convert(this System.Numerics.Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static (Vector2, Vector2) CrossSection(Vector2 ray, Vector2 origin, RectangleF bounds)
        {
            bounds.Offset(-origin);
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

        public static (Vector2 scale, float rotation, Vector2 translation, float skew) Decompose(this Matrix3x2 m)
        {
            var scale = m.GetScale();
            var translation = m.Row3;
            var skewx = (float) Math.Atan2(m.M12, m.M11);
            var skewy = (float) Math.Atan2(m.M22, m.M21) - PiOverTwo;
            var rotation = Wrap(skewx, TwoPi);
            var skew = Wrap(skewy - skewx, TwoPi);
            scale.Y *= (float) Math.Cos(skew);

            return (scale, rotation, translation, -skew);
        }

        public static float GetRotation(this Matrix3x2 m)
        {
            return (float) Math.Atan2(m.M12, m.M11);
        }

        public static Vector2 GetScale(this Matrix3x2 m)
        {
            return new Vector2(m.Row1.Length(), m.Row2.Length());
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
            return Vector2.Dot(a, b) / Vector2.Dot(b, b) * b;
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

        public static Rectangle Round(this RectangleF rect)
        {
            return new Rectangle((int) Math.Round(rect.X), (int) Math.Round(rect.Y),
                (int) Math.Round(rect.Width), (int) Math.Round(rect.Height));
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

        public static Vector2 Transform2D(Vector2 v, Matrix3x2 m)
        {
            return Matrix3x2.TransformPoint(m, v);
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