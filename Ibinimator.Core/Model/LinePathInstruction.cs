using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    [DebuggerDisplay("L {" + nameof(Position) + "}")]
    public class LinePathInstruction : CoordinatePathInstruction
    {
        public LinePathInstruction(float x, float y) : base(x, y) { }

        public LinePathInstruction(Vector2 position) : this(position.X, position.Y) { }
    }
}