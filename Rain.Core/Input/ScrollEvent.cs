using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public class ScrollEvent : InputEventBase
    {
        public ScrollEvent(
            float delta, Vector2 position, ScrollDirection direction, ModifierState modifiers)
        {
            Delta = delta;
            Direction = direction;
            ModifierState = modifiers;
            Position = position;
        }

        public float Delta { get; }
        public ScrollDirection Direction { get; }
        public ModifierState ModifierState { get; }
        public Vector2 Position { get; }
    }
}