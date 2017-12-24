using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public class PointerEvent : InputEventBase
    {
        public PointerEvent(Vector2 position, Vector2 delta, ModifierState modifierState)
        {
            Position = position;
            Delta = delta;
            ModifierState = modifierState;
        }

        public Vector2 Delta { get; }
        public ModifierState ModifierState { get; }
        public Vector2 Position { get; }
    }
}