using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public struct ModifierState
    {
        public ModifierState(
            bool control, bool shift, bool alt, bool leftMouse, bool middleMouse, bool rightMouse, bool xMouse1,
            bool xMouse2) : this()
        {
            Control = control;
            Shift = shift;
            Alt = alt;
            LeftMouse = leftMouse;
            MiddleMouse = middleMouse;
            RightMouse = rightMouse;
            XMouse1 = xMouse1;
            XMouse2 = xMouse2;
        }

        public bool Alt { get; }

        public bool Control { get; }

        public bool LeftMouse { get; }
        public bool MiddleMouse { get; }
        public bool RightMouse { get; }
        public bool Shift { get; }

        public bool XMouse1 { get; }
        public bool XMouse2 { get; }
    }
}