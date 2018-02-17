using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.Geometry
{
    [DebuggerDisplay("M {" + nameof(Position) + "}")]
    public class MovePathInstruction : CoordinatePathInstruction
    {
        public MovePathInstruction(float x, float y) : base(x, y) { }

        public MovePathInstruction(Vector2 position) : this(position.X, position.Y) { }
    }
}