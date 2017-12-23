using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public class ScrollEvent : InputEventBase
    {
        public ScrollEvent(float delta, ScrollDirection direction, ModifierState modifiers)
        {
            Delta = delta;
            Direction = direction;
            ModifierState = modifiers;
        }

        public float Delta { get; }
        public ScrollDirection Direction { get; }
        public ModifierState ModifierState { get; }
    }
}