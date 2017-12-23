using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
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