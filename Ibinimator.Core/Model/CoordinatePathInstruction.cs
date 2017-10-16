using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public abstract class CoordinatePathInstruction : PathInstruction
    {
        protected CoordinatePathInstruction(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
        public Vector2 Position => new Vector2(X, Y);
    }
}