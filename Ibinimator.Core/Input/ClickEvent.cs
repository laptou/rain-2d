using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public enum ScrollDirection
    {
        Horizontal,
        Vertical
    }

    public class ClickEvent : InputEventBase
    {
        public ClickEvent(Vector2 position, MouseButton mouseButton, ClickType type, ModifierState modifiers)
        {
            MouseButton = mouseButton;
            Type = type;
            State = type != ClickType.Up;
            ModifierState = modifiers;
            Position = position;
        }

        public ModifierState ModifierState { get; }

        public MouseButton MouseButton { get; }

        public Vector2 Position { get; }

        public bool State { get; }

        public ClickType Type { get; }
    }

    public enum ClickType
    {
        Up,
        Down,
        Double
    }
}