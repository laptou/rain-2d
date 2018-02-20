using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public class KeyboardEvent : InputEventBase
    {
        public KeyboardEvent(int keyCode, bool state, ModifierState modifierState)
        {
            KeyCode = keyCode;
            State = state;
            ModifierState = modifierState;
        }

        public int KeyCode { get; }

        public ModifierState ModifierState { get; }

        public bool State { get; }
    }
}