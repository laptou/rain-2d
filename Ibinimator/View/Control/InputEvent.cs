using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ibinimator.View.Control {
    public struct InputEvent
    {
        public InputEvent(InputEventType type, Key key, ModifierKeys modifier) : this(
            Service.Time.Now)
        {
            if (type != InputEventType.KeyDown &&
                type != InputEventType.KeyUp)
                throw new ArgumentException(nameof(type));

            Type = type;
            Key = key;
            Modifier = modifier;
        }

        public InputEvent(InputEventType type, string text) : this(Service.Time.Now)
        {
            if (type != InputEventType.TextInput)
                throw new ArgumentException(nameof(type));

            Type = type;
            Text = text;
        }

        public InputEvent(
            InputEventType type,
            bool left,
            bool middle,
            bool right,
            Vector2 position) : this(Service.Time.Now)
        {
            if (type != InputEventType.MouseUp &&
                type != InputEventType.MouseDown)
                throw new ArgumentException(nameof(type));

            Type = type;
            LeftMouse = left;
            MiddleMouse = middle;
            RightMouse = right;
            Position = position;
        }

        public InputEvent(InputEventType type, Vector2 position) : this(Service.Time.Now)
        {
            if (type != InputEventType.MouseMove)
                throw new ArgumentException(nameof(type));

            Type = type;
            Position = position;
        }

        public InputEvent(InputEventType type, float delta, Vector2 position) : this(Service.Time.Now)
        {
            if (type != InputEventType.ScrollVertical &&
                type != InputEventType.ScrollHorizontal)
                throw new ArgumentException(nameof(type));

            Type = type;
            ScrollDelta = delta;
            Position = position;
        }

        private InputEvent(long time) : this() { Time = time; }

        public Key Key { get; }
        public bool LeftMouse { get; }
        public bool MiddleMouse { get; }
        public ModifierKeys Modifier { get; }
        public Vector2 Position { get; }
        public bool RightMouse { get; }
        public string Text { get; }
        public long Time { get; }
        public float ScrollDelta { get; }
        public InputEventType Type { get; }
    }
}