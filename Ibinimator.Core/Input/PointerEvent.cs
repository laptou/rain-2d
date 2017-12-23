using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public class PointerEvent : InputEventBase
    {
        public PointerEvent(Vector2 position, Vector2 delta, ModifierState state)
        {
            Position = position;
            Delta = delta;
            State = state;
        }

        public Vector2 Delta { get; }
        public ModifierState State { get; }
        public Vector2 Position { get; }
    }
}