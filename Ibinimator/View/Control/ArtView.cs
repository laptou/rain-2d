using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Renderer.WPF;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service;
using Ibinimator.Service.Tools;
using SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage
    {
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);
        private readonly Queue<InputEvent> _events = new Queue<InputEvent>();
        private bool _eventLoop;

        private Vector2 _lastPosition;
        private ISet<Key> _keys = new HashSet<Key>();

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            Focusable = true;

            RenderTargetBound += OnRenderTargetBound;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ArtContext = new ArtContext(this);
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            lock (_events)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed ||
                    Mouse.MiddleButton == MouseButtonState.Pressed ||
                    Mouse.RightButton == MouseButtonState.Pressed)
                    _events.Enqueue(
                        new InputEvent(
                            InputEventType.MouseUp,
                            true,
                            true,
                            true,
                            Vector2.Zero));
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            Trace.WriteLine($"Lost key focus: {string.Join(", ", _keys)}");

            lock (_events)
            {
                foreach (var key in _keys)
                {
                    _events.Enqueue(
                        new InputEvent(
                            InputEventType.KeyUp,
                            key,
                            Keyboard.Modifiers));
                }
            }

            _keys.Clear();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            Keyboard.Focus(this);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            Keyboard.ClearFocus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            Trace.WriteLine($"Key down: {key}");

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(InputEventType.KeyDown, key, Keyboard.Modifiers));
            }

            _eventFlag.Set();

            _keys.Add(key);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            Trace.WriteLine($"Key up: {key}");

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(InputEventType.KeyUp, key, Keyboard.Modifiers));
            }

            _eventFlag.Set();

            _keys.Remove(key);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            if (e.MiddleButton == MouseButtonState.Pressed) return;

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.MouseDown,
                        e.LeftButton == MouseButtonState.Pressed,
                        e.MiddleButton == MouseButtonState.Pressed,
                        e.RightButton == MouseButtonState.Pressed,
                        e.GetPosition(this).Convert()));
            }

            _eventFlag.Set();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos = e.GetPosition(this).Convert();

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                var artPosLast = ArtContext.ViewManager.ToArtSpace(_lastPosition);
                var artPos = ArtContext.ViewManager.ToArtSpace(new Vector2(pos.X, pos.Y));

                ArtContext.ViewManager.Pan += artPos - artPosLast;

                _lastPosition = pos;

                ArtContext.InvalidateSurface();

                return;
            }

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.MouseMove,
                        pos));
            }

            _eventFlag.Set();

            _lastPosition = pos;
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.MouseUp,
                        e.LeftButton == MouseButtonState.Pressed,
                        e.MiddleButton == MouseButtonState.Pressed,
                        e.RightButton == MouseButtonState.Pressed,
                        e.GetPosition(this).Convert()));
            }

            if (e.MiddleButton == MouseButtonState.Pressed) return;

            _eventFlag.Set();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (ArtContext.ViewManager == null) return;

            var vm = ArtContext.ViewManager;
            var scale = 1 + e.Delta / 500f;
            var pos = e.GetPosition(this).Convert();
            var pan = e.Delta / (vm.Zoom * vm.Zoom);

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                vm.Zoom *= scale;
                vm.Pan += pos * (Vector2.One - new Vector2(scale)) / 2;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                vm.Pan += Vector2.UnitX * pan;
            else vm.Pan += Vector2.UnitY * pan;

            ArtContext.InvalidateSurface();

            OnMouseWheel(e);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            ArtContext.CacheManager?.ResetAll();
            ArtContext.CacheManager?.LoadBrushes(RenderContext);
            ArtContext.CacheManager?.LoadBitmaps(RenderContext);

            if (ArtContext.ViewManager?.Root != null)
                ArtContext.CacheManager?.Bind(ArtContext.ViewManager.Document);

            InvalidateVisual();
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            // quickfix b/c asychronous events mean we can't use Handled
            // which creates the problem of backspace registering as 
            // text input for some reason
            if (e.Text == "\b" || e.Text == "")
            {
                _eventFlag.Set();
                return;
            }

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.TextInput,
                        e.Text));
            }

            _eventFlag.Set();
        }

        protected override void Render(RenderContext target)
        {
            target.Clear(new Color(0.5f));

            if (ArtContext.ViewManager == null) return;

            using (ArtContext.CacheManager.Lock())
            {
                var ac = ArtContext;

                target.Transform(ac.ViewManager.Transform, true);

                ac.ViewManager.Render(target, ac.CacheManager);

                ac.ViewManager.Root.Render(target, ac.CacheManager, ac.ViewManager);

                if (ac.SelectionManager == null) return;

                ac.SelectionManager.Render(target, ac.CacheManager);

                if (ac.ToolManager?.Tool == null) return;

                ac.ToolManager.Tool.Render(target, ac.CacheManager, ac.ViewManager);

                if (ac.ToolManager.Tool.CursorImage == null)
                {
                    Cursor = Cursors.Arrow;
                    return;
                }

                Cursor = Cursors.None;

                target.Transform(
                    Matrix3x2.CreateRotation(ac.ToolManager.Tool.CursorRotate,
                                             new Vector2(8)) *
                    Matrix3x2.CreateTranslation(_lastPosition - new Vector2(8)),
                    true);

                target.DrawBitmap(ac.CacheManager.GetBitmap(ac.ToolManager.Tool.CursorImage));
            }
        }

        private void EventLoop()
        {
            var ac = ArtContext;

            while (_eventLoop)
            {
                while (_events.Count > 0)
                {
                    var evt = _events.Dequeue();

                    var pos = ac.ViewManager.ToArtSpace(evt.Position);

                    switch (evt.Type)
                    {
                        case InputEventType.MouseDown:
                            if (!ac.ToolManager.MouseDown(pos))
                                ac.SelectionManager.MouseDown(pos);
                            break;
                        case InputEventType.MouseUp:
                            if (!ac.ToolManager.MouseUp(pos))
                                ac.SelectionManager.MouseUp(pos);
                            break;
                        case InputEventType.MouseMove:
                            if (Time.Now - evt.Time > 16
                            ) // at 16ms, begin skipping mouse moves
                            {
                                lock (_events)
                                {
                                    if (_events.Any() &&
                                        _events.Peek().Type == InputEventType.MouseMove)
                                        continue;
                                }
                            }

                            if (!ac.ToolManager.MouseMove(pos))
                                ac.SelectionManager.MouseMove(pos);
                            break;

                        case InputEventType.TextInput:
                            ac.ToolManager.TextInput(evt.Text);
                            break;
                        case InputEventType.KeyUp:
                            if (!ac.ToolManager.KeyUp(evt.Key, evt.Modifier))
                                ac.SelectionManager.KeyUp(evt.Key, evt.Modifier);
                            break;
                        case InputEventType.KeyDown:
                            if (!ac.ToolManager.KeyDown(evt.Key, evt.Modifier))
                                ac.SelectionManager.KeyDown(evt.Key, evt.Modifier);
                            break;
                        case InputEventType.Scroll:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _eventFlag.Reset();

                //Dispatcher.Invoke(() => Cursor = ToolManager?.Tool?.Cursor != null ? Cursors.None : Cursors.Arrow,
                //    DispatcherPriority.Render);

                _eventFlag.WaitOne(5000);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _eventLoop = true;

            var evtThread = new Thread(EventLoop);
            evtThread.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ArtContext.CacheManager?.ResetAll();
            _eventLoop = false;
        }

        #region IArtContext Members

        public ArtContext ArtContext { get; }

        #endregion
    }

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
            Vector2 position) : this(Service.Time
                                            .Now)
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
            if (type != InputEventType.MouseMove &&
                type != InputEventType.Scroll)
                throw new ArgumentException(nameof(type));

            Type = type;
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
        public InputEventType Type { get; }
    }

    public enum InputEventType
    {
        TextInput,
        KeyUp,
        KeyDown,
        MouseUp,
        MouseDown,
        MouseMove,
        Scroll
    }
}