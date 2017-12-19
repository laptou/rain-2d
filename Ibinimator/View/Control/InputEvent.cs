using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core.Model;

namespace Ibinimator.View.Control
{
    public struct InputEvent
    {
        public InputEvent(InputEventType type, Key key, ModifierState state) : this(
            Service.Time.Now, state)
        {
            if (type != InputEventType.KeyDown &&
                type != InputEventType.KeyUp)
                throw new ArgumentException(nameof(type));

            Type = type;
            Key = key;
            State = state;
        }

        public InputEvent(InputEventType type, string text) : this(Service.Time.Now)
        {
            if (type != InputEventType.TextInput)
                throw new ArgumentException(nameof(type));

            Type = type;
            Text = text;
        }

        public InputEvent(InputEventType type, Vector2 position, ModifierState modifiers) : this(
            Service.Time.Now, modifiers)
        {
            Type = type;
            Position = position;
        }

        public InputEvent(InputEventType type, float delta, Vector2 position, ModifierState modifiers) : this(
            Service.Time.Now, modifiers)
        {
            if (type != InputEventType.ScrollVertical &&
                type != InputEventType.ScrollHorizontal)
                throw new ArgumentException(nameof(type));

            Type = type;
            ScrollDelta = delta;
            Position = position;
        }

        private InputEvent(long time, [Optional] ModifierState state) : this()
        {
            Time = time;
            State = state;
        }

        public Key            Key         { get; }
        public Vector2        Position    { get; }
        public float          ScrollDelta { get; }
        public ModifierState  State       { get; }
        public string         Text        { get; }
        public long           Time        { get; }
        public InputEventType Type        { get; }
    }
}