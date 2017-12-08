using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    [DebuggerDisplay("R: {R}, G: {G}, B: {B}, A: {A}")]
    public struct Color
    {
        public Color(Vector4 v) : this(v.X, v.Y, v.Z, v.W)
        {
        }

        public Color(float r, float g, float b, float a) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(float r, float g, float b) : this(r, g, b, 1)
        {
        }

        public Color(float f) : this(f, f, f)
        {
            
        }

        public float A { get; set; }
        public float B { get; set; }
        public float G { get; set; }
        public float R { get; set; }

        public Vector4 AsVector()
        {
            return new Vector4(R, G, B, A);
        }

        public static Vector4 operator -(Color c1, Color c2) { return c1.AsVector() - c2.AsVector(); }

        public static Color operator +(Color c, Vector4 v) { return new Color(c.AsVector() + v); }
    }
}