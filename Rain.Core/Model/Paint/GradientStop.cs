using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public struct GradientStop
    {
        public GradientStop(Color color, float offset) : this()
        {
            Color = color;
            Offset = offset;
        }

        public Color Color { get; set; }
        public float Offset { get; set; }
    }
}