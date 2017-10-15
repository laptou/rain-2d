using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;

namespace Ibinimator.Renderer.Direct2D
{
    public static class Direct2DConverter
    {
        public static SharpDX.Color4 Convert(this Color color)
        {
            return new SharpDX.Color4(color.R, color.G, color.B, color.A);
        }

        public static SharpDX.Vector2 Convert(this Vector2 vec)
        {
            return new SharpDX.Vector2(vec.X, vec.Y);
        }

        public static Vector2 Convert(this SharpDX.Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Matrix3x2 Convert(this SharpDX.Mathematics.Interop.RawMatrix3x2 mat)
        {
            return Convert((SharpDX.Matrix3x2) mat);
        }

        public static SharpDX.Matrix3x2 Convert(this Matrix3x2 mat)
        {
            return new SharpDX.Matrix3x2(mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32);
        }

        public static Matrix3x2 Convert(this SharpDX.Matrix3x2 mat)
        {
            return new Matrix3x2(mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32);
        }
    }
}