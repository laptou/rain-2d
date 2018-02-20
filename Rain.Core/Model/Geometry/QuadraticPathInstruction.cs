using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Geometry
{
    public class QuadraticPathInstruction : CoordinatePathInstruction
    {
        public QuadraticPathInstruction(float x, float y, float controlX, float controlY) :
            base(x, y)
        {
            ControlX = controlX;
            ControlY = controlY;
        }

        public QuadraticPathInstruction(Vector2 position, Vector2 control) : this(
            position.X,
            position.Y,
            control.X,
            control.Y) { }

        public Vector2 Control => new Vector2(ControlX, ControlY);

        public float ControlX { get; }
        public float ControlY { get; }
    }
}