using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public struct Color
    {
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
    }
}