using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public class FocusEvent : InputEventBase
    {
        public FocusEvent(bool state, ModifierState modifiers)
        {
            State = state;
            ModifierState = modifiers;
        }

        public ModifierState ModifierState { get; }

        public bool State { get; }
    }
}