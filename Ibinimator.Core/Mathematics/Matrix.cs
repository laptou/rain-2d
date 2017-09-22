using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Mathematics
{
    public struct Matrix
    {
        public static readonly Matrix Identity = new Matrix(1, 0, 0, 0, 1, 0);
        public float M11 { get; }
        public float M12 { get; }
        public float M13 { get; }
        public float M21 { get; }
        public float M22 { get; }
        public float M23 { get; }

        public Matrix(float m11, float m12, float m13, float m21, float m22, float m23)
        {
            M11 = m11;
            M21 = m21;
            M12 = m12;
            M22 = m22;
            M13 = m13;
            M23 = m23;
        }

        public Matrix(IEnumerable<float> values)
        {
            var arr = values.ToArray();

            if(arr.Length != 6) throw new ArgumentException();

            M11 = arr[0];
            M12 = arr[1];
            M13 = arr[2];
            M21 = arr[3];
            M22 = arr[4];
            M23 = arr[5];
        }

        public (Vector2 scale, float rotation, Vector2 translation, float shear) Decompose()
        {
            return (Scale(), Rotation(), Translation(), Shear());
        }

        public float Determinant()
        {
            return M11 * M22 - M12 * M21; // very simple bc most terms are always 0
        }

        public Matrix Inverse()
        {
            float
                a = M22,
                b = -M21,
                d = -M12,
                e = M11,
                g = M12 * M23 - M13 * M22,
                h = M11 * M23 - M13 * M21,
                i = M11 * M22 - M12 * M21;

            return 1 / Determinant() * new Matrix(a, d, g, b, e, h) / i;
        }

        public static Matrix Rotate(float a)
        {
            var cos = (float) Math.Cos(a);
            var sin = (float) Math.Sin(a);
            return new Matrix(cos, -sin, 0, sin, cos, 0);
        }

        public static Matrix Rotate(float a, float cx, float cy)
        {
            return Translate(-cx, -cy) * Rotate(a) * Translate(cx, cy);
        }

        public static Matrix Rotate(float a, Vector2 c)
        {
            return Rotate(a, c.X, c.Y);
        }

        public float Rotation()
        {
            const float twopi = (float)(Math.PI * 2);
            return (float) (Math.Atan2(M21, M11) + twopi) % twopi;
        }

        public Vector2 Scale()
        {
            return new Vector2(
                (float) Math.Sqrt(M11 * M11 + M12 * M12),
                (float) Math.Sqrt(M22 * M22 + M21 * M21));
        }

        public static Matrix Scale(float x, float y)
        {
            return new Matrix(x, 0, 0, 0, y, 0);
        }

        public static Matrix Scale(Vector2 s)
        {
            return Scale(s.X, s.Y);
        }

        public static Matrix Scale(float s)
        {
            return Scale(s, s);
        }

        public static Matrix Scale(float x, float y, float cx, float cy)
        {
            return Translate(-cx, -cy) * Scale(x, y) * Translate(cx, cy);
        }

        public static Matrix Scale(float x, float y, Vector2 c)
        {
            return Scale(x, y, c.X, c.Y);
        }

        public static Matrix Scale(Vector2 s, Vector2 c)
        {
            return Scale(s.X, s.Y, c.X, c.Y);
        }

        public float Shear()
        {
            const float twopi = (float)(Math.PI * 2);
            var s = -Math.Atan2(M22, M12) + Math.PI / 2 + Math.Atan2(M21, M11);
            return (float) (s + twopi) % twopi;
        }

        public static Matrix Shear(float x, float y)
        {
            return new Matrix(1, (float) Math.Tan(x), 0, (float) Math.Tan(y), 1, 0);
        }

        public static Matrix Shear(float x, float y, float cx, float cy)
        {
            return Translate(-cx, -cy) * Shear(x, y) * Translate(cx, cy);
        }

        public static Matrix Shear(float x, float y, Vector2 c)
        {
            return Shear(x, y, c.X, c.Y);
        }

        public static Matrix Shear(Vector2 s, Vector2 c)
        {
            return Shear(s.X, s.Y, c.X, c.Y);
        }

        public override string ToString()
        {
            return $"[\t{M11}\t\t{M12}\t\t{M13}\t]\n[\t{M21}\t\t{M22}\t\t{M23}\t]";
        }

        public static Matrix Translate(float x, float y)
        {
            return new Matrix(1, 0, x, 0, 1, y);
        }

        public static Matrix Translate(Vector2 t)
        {
            return Translate(t.X, t.Y);
        }

        public static Matrix Translate(float t)
        {
            return Translate(t, t);
        }

        public Vector2 Translation()
        {
            return new Vector2(M13, M23);
        }

        public static Matrix operator +(Matrix me, Matrix them)
        {
            return new Matrix(
                me.M11 + them.M11,
                me.M12 + them.M12,
                me.M13 + them.M13,
                me.M21 + them.M21,
                me.M22 + them.M22,
                me.M23 + them.M23);
        }

        public static Matrix operator /(Matrix me, float them)
        {
            return new Matrix(
                me.M11 / them,
                me.M12 / them,
                me.M13 / them,
                me.M21 / them,
                me.M22 / them,
                me.M23 / them);
        }

        //public static implicit operator Matrix3x2(Matrix mat)
        //{
        //    return new Matrix3x2(mat.M11, mat.M21, mat.M12, mat.M22, mat.M13, mat.M23);
        //}

        //public static implicit operator Matrix(Matrix3x2 mat)
        //{
        //    return new Matrix(mat.M11, mat.M21, mat.M31, mat.M12, mat.M22, mat.M32);
        //}

        public static Matrix operator *(Matrix me, float them)
        {
            return new Matrix(
                me.M11 * them,
                me.M12 * them,
                me.M13 * them,
                me.M21 * them,
                me.M22 * them,
                me.M23 * them);
        }

        public static Matrix operator *(float them, Matrix me)
        {
            return me * them;
        }

        public static Matrix operator *(Matrix me, Matrix them)
        {
            // row 3 is always 0 0 1 so M31 = 0, M32 = 0, M33 = 1
            var m11 = (double) me.M11 * them.M11 + (double) me.M12 * them.M21;
            var m12 = (double) me.M11 * them.M12 + (double) me.M12 * them.M22;
            var m13 = (double) me.M11 * them.M13 + (double) me.M12 * them.M23 + me.M13;
            var m21 = (double) me.M21 * them.M11 + (double) me.M22 * them.M21;
            var m22 = (double) me.M21 * them.M12 + (double) me.M22 * them.M22;
            var m23 = (double) me.M21 * them.M13 + (double) me.M22 * them.M23 + me.M23;
            return new Matrix((float) m11, (float) m12, (float) m13, (float) m21, (float) m22, (float) m23);
        }

        public static Vector2 operator *(Matrix me, Vector2 them)
        {
            var x = (double) me.M11 * them.X + (double) me.M12 * them.Y + me.M13;
            var y = (double) me.M22 * them.Y + (double) me.M21 * them.X + me.M23;
            return new Vector2((float) x, (float) y);
        }

        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return M11;
                    case 1: return M21;
                    case 2: return M12;
                    case 3: return M22;
                    case 4: return M13;
                    case 5: return M23;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}