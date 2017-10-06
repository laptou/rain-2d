using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public class QuadraticPathInstruction : CoordinatePathInstruction
    {
        public QuadraticPathInstruction(float x, float y, float controlX, float controlY) : base(x, y)
        {
            ControlX = controlX;
            ControlY = controlY;
        }

        public float ControlX { get; }
        public float ControlY { get; }
    }
}