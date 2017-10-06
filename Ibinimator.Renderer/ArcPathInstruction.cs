using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public class ArcPathInstruction : CoordinatePathInstruction
    {
        public ArcPathInstruction(float x, float y, float radiusX, float radiusY, float angle, bool clockwise,
            bool largeArc) : base(x, y)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
            Angle = angle;
            Clockwise = clockwise;
            LargeArc = largeArc;
        }

        public float Angle { get; }
        public bool Clockwise { get; }
        public bool LargeArc { get; }

        public float RadiusX { get; }
        public float RadiusY { get; }
    }
}