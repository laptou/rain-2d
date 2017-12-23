using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    [DebuggerDisplay("C {Control1} {Control2} {" + nameof(Position) + "}")]
    public class CubicPathInstruction : CoordinatePathInstruction
    {
        public CubicPathInstruction(
            float x, float y, float control1X, float control1Y, float control2X,
            float control2Y) : base(x, y)
        {
            Control1X = control1X;
            Control1Y = control1Y;
            Control2X = control2X;
            Control2Y = control2Y;
        }

        public CubicPathInstruction(Vector2 position, Vector2 control1, Vector2 control2) :
            this(position.X, position.Y, control1.X, control1.Y, control2.X, control2.Y) { }

        public Vector2 Control1 => new Vector2(Control1X, Control1Y);

        public float Control1X { get; }
        public float Control1Y { get; }
        public Vector2 Control2 => new Vector2(Control2X, Control2Y);

        public float Control2X { get; }
        public float Control2Y { get; }
    }
}