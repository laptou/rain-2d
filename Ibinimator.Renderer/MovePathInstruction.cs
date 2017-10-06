using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public class MovePathInstruction : CoordinatePathInstruction
    {
        public MovePathInstruction(float x, float y) : base(x, y)
        {
        }
    }
}