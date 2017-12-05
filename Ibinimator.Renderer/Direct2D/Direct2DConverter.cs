using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics.Interop;
using Color = Ibinimator.Core.Model.Color;
using Matrix3x2 = System.Numerics.Matrix3x2;

namespace Ibinimator.Renderer.Direct2D
{
    public static class Direct2DConverter
    {
        public static Color4 Convert(this Color color) { return new Color4(color.R, color.G, color.B, color.A); }

        public static Vector2 Convert(this System.Numerics.Vector2 vec) { return new Vector2(vec.X, vec.Y); }

        public static System.Numerics.Vector2 Convert(this Vector2 vec)
        {
            return new System.Numerics.Vector2(vec.X, vec.Y);
        }

        public static Matrix3x2 Convert(this RawMatrix3x2 mat) { return Convert((SharpDX.Matrix3x2) mat); }

        public static SharpDX.Matrix3x2 Convert(this Matrix3x2 mat)
        {
            return new SharpDX.Matrix3x2(mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32);
        }

        public static Matrix3x2 Convert(this SharpDX.Matrix3x2 mat)
        {
            return new Matrix3x2(mat.M11, mat.M12, mat.M21, mat.M22, mat.M31, mat.M32);
        }

        public static RectangleF Convert(this Core.Model.RectangleF r)
        {
            return new RectangleF(r.Left, r.Top, r.Width, r.Height);
        }

        public static Core.Model.RectangleF Convert(this RectangleF r)
        {
            return new Core.Model.RectangleF(r.Left, r.Top, r.Width, r.Height);
        }
    }
}