using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.Geometry
{
    [DebuggerDisplay("A {RadiusX} {RadiusY} {Angle} {Clockwise} {LargeArc} {" + nameof(Position) +
                     "}")]
    public class ArcPathInstruction : CoordinatePathInstruction
    {
        public ArcPathInstruction(
            float x, float y, float radiusX, float radiusY, float angle, bool clockwise,
            bool largeArc) : base(x, y)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
            Angle = angle;
            Clockwise = clockwise;
            LargeArc = largeArc;
        }

        public ArcPathInstruction(
            Vector2 position, Vector2 radii, float angle, bool clockwise, bool largeArc) : this(
            position.X,
            position.Y,
            radii.X,
            radii.Y,
            angle,
            clockwise,
            largeArc) { }

        public float Angle { get; }
        public bool Clockwise { get; }
        public bool LargeArc { get; }

        public float RadiusX { get; }
        public float RadiusY { get; }
    }
}