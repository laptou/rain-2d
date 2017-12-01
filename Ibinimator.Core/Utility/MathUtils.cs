using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static System.Math;

namespace Ibinimator.Core.Utility
{
    public static class MathUtils
    {
        public const float TwoPi = (float) PI * 2;
        public const float PiOverTwo = (float) PI / 2;
        public const float Pi = (float) PI;
        public const float PiOverFour = PiOverTwo / 2;
        public const float Epsilon = 0.0001f;

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

        public static float Angle(Vector2 pos, bool reverse)
        {
            return (float) Atan2(reverse ? -pos.Y : pos.Y, pos.X);
        }

        public static (float left, float top, float right, float bottom) Bounds(
            (float left, float top, float right, float bottom) rect,
            Matrix3x2 m)
        {
            Vector2 p0 = Vector2.Transform(new Vector2(rect.left, rect.top), m),
                    p1 = Vector2.Transform(new Vector2(rect.right, rect.top), m),
                    p2 = Vector2.Transform(new Vector2(rect.right, rect.bottom), m),
                    p3 = Vector2.Transform(new Vector2(rect.left, rect.bottom), m);

            float l = Min(p0.X, p1.X, p2.X, p3.X),
                  t = Min(p0.Y, p1.Y, p2.Y, p3.Y),
                  r = Max(p0.X, p1.X, p2.X, p3.X),
                  b = Max(p0.Y, p1.Y, p2.Y, p3.Y);

            return (l, t, r, b);
        }

        public static T Clamp<T>(T min, T max, T value) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static bool ContainsNaN(Matrix3x2 mat)
        {
            return new[] {mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32}.Any(
                float.IsNaN);
        }

        public static (Vector2, Vector2) CrossSection(
            Vector2 ray,
            Vector2 origin,
            (float Left, float Top, float Right, float Bottom) bounds)
        {
            bounds = (
                bounds.Left - origin.X,
                bounds.Top - origin.Y,
                bounds.Right - origin.X,
                bounds.Bottom - origin.Y);

            Vector2 v1;
            Vector2 v2;
            var r = Abs(ray);
            var o = Abs(new Vector2(bounds.Right, bounds.Top));
            var angle = Atan2(-r.Y, r.X);
            var tan = (float) Tan(angle);

            if (Math.Abs(angle) > Math.Abs(Atan2(-o.Y, o.X)))
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

        public static (Vector2 scale, float rotation, Vector2 translation, float skew)
            Extract(this Matrix3x2 m)
        {
            var scale = m.GetScale();
            var translation = m.Translation;
            var skewx = m.GetRotation();
            var skewy = (float) Atan2(m.M22, m.M21) - PiOverTwo;
            var rotation = Wrap(skewx, TwoPi);
            var skew = Wrap(skewy - skewx, TwoPi);
            scale.Y *= (float) Cos(skew);

            return (scale, rotation, translation, -skew);
        }

        public static (Matrix3x2 scale, Matrix3x2 rotate, Matrix3x2 translate)
            Decompose(this Matrix3x2 m)
        {
            var scale = m.GetScale() * Sign((m.M11, m.M22));
            var s = new Matrix3x2(scale.X, 0, 0, scale.Y, 0, 0);
            var r = new Matrix3x2(m.M11 / scale.X,
                                  m.M12 / scale.X,
                                  m.M21 / scale.Y,
                                  m.M22 / scale.Y,
                                  0,
                                  0);

            return (s, r, Matrix3x2.CreateTranslation(m.M31,
                                                      m.M32));
        }

        public static float GetRotation(this Matrix3x2 m)
        {
            return Wrap((float) Atan2(m.M12, m.M11), TwoPi);
        }

        public static Vector2 GetScale(this Matrix3x2 m)
        {
            return new Vector2(
                (float) Math.Sqrt((double) m.M11 * m.M11 + (double) m.M12 * m.M12),
                (float) Math.Sqrt((double) m.M21 * m.M21 + (double) m.M22 * m.M22));
        }

        public static float GetShear(this Matrix3x2 m)
        {
            return 0;
            var shear = (float) Atan2(m.M22, m.M21) - PiOverTwo;
            return -Wrap(shear - GetRotation(m), -Pi, Pi);
        }

        public static Matrix3x2 Invert(Matrix3x2 mat)
        {
            Matrix3x2.Invert(mat, out mat);
            return mat;
        }

        public static T Max<T>(params T[] x) { return x.Max(); }

        public static T Min<T>(params T[] x) { return x.Min(); }

        public static float NonZeroSign(float f) { return f >= 0 ? 1 : -1; }

        public static Vector2 NonZeroSign(Vector2 v)
        {
            return new Vector2(NonZeroSign(v.X), NonZeroSign(v.Y));
        }

        /// <summary>
        /// Returns the projection of vector <paramref name="a"/>
        /// onto vector <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The vector to project.</param>
        /// <param name="b">The vector to project onto.</param>
        /// <returns>The projection of a onto b.</returns>
        public static Vector2 Project(Vector2 a, Vector2 b)
        {
            return Vector2.Dot(a, b) / Vector2.Dot(b, b) * b;
        }

        /// <summary>
        /// Rotates vector <paramref name="v"/> by 
        /// <paramref name="theta"/> radians.
        /// </summary>
        /// <param name="v">The vector to rotate.</param>
        /// <param name="theta">The angle to rotate the vector by.</param>
        /// <returns>The rotated vector.</returns>
        public static Vector2 Rotate(Vector2 v, float theta)
        {
            var cs = (float) Cos(theta);
            var sn = (float) Sin(theta);

            var px = v.X * cs - v.Y * sn;
            var py = v.X * sn + v.Y * cs;

            return new Vector2(px, py);
        }

        /// <summary>
        /// Rotates vector <paramref name="v"/> by
        /// <paramref name="theta"/> radians around 
        /// center of rotation <paramref name="c"/>.
        /// </summary>
        /// <param name="v">The vector to rotate.</param>
        /// <param name="c">The center of rotation.</param>
        /// <param name="theta">The angle to rotate the vector by.</param>
        /// <returns>The rotated vector.</returns>
        public static Vector2 Rotate(Vector2 v, Vector2 c, float theta)
        {
            return Rotate(v - c, theta) + c;
        }

        public static Vector2 Scale(Vector2 v, Vector2 c, Vector2 s)
        {
            return (v - c) * s + c;
        }

        public static Vector2 ShearX(Vector2 v, float theta)
        {
            return new Vector2(v.X + (float) Tan(theta) * v.Y, v.Y);
        }

        public static Vector2 ShearX(Vector2 v, Vector2 c, float theta)
        {
            return ShearX(v - c, theta) + c;
        }

        public static Vector2 Sign(Vector2 v)
        {
            return new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
        }

        public static Vector2 Sign((float X, float Y) v)
        {
            return new Vector2(Math.Sign(v.X), Math.Sign(v.Y));
        }

        public static Vector2 Sqrt(Vector2 v)
        {
            return new Vector2(AbsSqrt(v.X), AbsSqrt(v.Y));
        }

        public static Vector2 UnitVector(float angle)
        {
            return new Vector2((float) Cos(angle), (float) Sin(angle));
        }

        public static float Wrap(float f, float r) => Wrap(f, 0, r);

        public static float Wrap(float f, float min, float max) { return Math.Abs(f - min) % (max - min) + min; }

        public static int Wrap(int f, int r) { return (f % r + r) % r; }

        public static Vector2 Angle(float a)
        {
            return new Vector2((float) Cos(a), -(float) Sin(a));
        }

        public static float Round(float rotate, float floor, float ceiling)
        {
            var f = (rotate - floor) / (ceiling - floor);
            return (float) Math.Round(f) * (ceiling - floor) + floor;
        }
    }
}