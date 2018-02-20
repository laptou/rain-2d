using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public class TextEvent : InputEventBase
    {
        public TextEvent(string text, ModifierState modifiers)
        {
            Text = text;
            ModifierState = modifiers;
        }

        public ModifierState ModifierState { get; }

        public string Text { get; }
    }
}