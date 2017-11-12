using System.Threading.Tasks;
using System;
using System.Collections.Generic;
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
using SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage, IArtContext
    {
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);
        private readonly Queue<InputEvent> _events = new Queue<InputEvent>();
        private bool _eventLoop;

        private Vector2 _lastPosition;

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            Focusable = true;

            RenderTargetBound += OnRenderTargetBound;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public void SetManager<T>(T manager) where T : IArtContextManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            var managerInterfaces =
                typeof(T).FindInterfaces((type, criteria) =>
                                             typeof(IArtContextManager).IsAssignableFrom(
                                                 type),
                                         null)
                         .Concat(new[] {typeof(T)});

            var interfaces = managerInterfaces.ToList();

            if (interfaces.Contains(typeof(IBrushManager)))
                BrushManager = (IBrushManager) manager;

            if (interfaces.Contains(typeof(ICacheManager)))
            {
                CacheManager?.ResetAll();

                CacheManager = (ICacheManager) manager;

                if (RenderContext != null)
                {
                    CacheManager.LoadBrushes(RenderContext);
                    CacheManager.LoadBitmaps(RenderContext);

                    if (ViewManager?.Root != null)
                        CacheManager.Bind(ViewManager.Document);
                }
            }

            if (interfaces.Contains(typeof(IHistoryManager)))
                HistoryManager = (IHistoryManager) manager;

            if (interfaces.Contains(typeof(ISelectionManager)))
                SelectionManager = (ISelectionManager) manager;

            if (interfaces.Contains(typeof(IToolManager)))
                ToolManager = (IToolManager) manager;

            if (interfaces.Contains(typeof(IViewManager)))
            {
                ViewManager = (IViewManager) manager;
                ViewManager.DocumentUpdated +=
                    (s, e) => CacheManager?.Bind(ViewManager.Document);

                CacheManager?.ResetDeviceResources();
                if (ViewManager?.Root != null)
                    CacheManager?.Bind(ViewManager.Document);
            }

            InvalidateSurface();
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

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.KeyDown,
                        e.Key == Key.System ? e.SystemKey : e.Key,
                        Keyboard.Modifiers));
            }

            _eventFlag.Set();

            // e.Handled = true;
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.KeyUp,
                        e.Key == Key.System ? e.SystemKey : e.Key,
                        Keyboard.Modifiers));
            }

            _eventFlag.Set();
            // e.Handled = true;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

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

            ReleaseMouseCapture();

            _eventFlag.Set();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (ViewManager == null) return;

            var scale = 1 + e.Delta / 500f;
            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                ViewManager.Zoom *= scale;
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                ViewManager.Pan += new Vector2(e.Delta / 10f / ViewManager.Zoom, 0);
            else
                ViewManager.Pan += new Vector2(0, e.Delta / 10f / ViewManager.Zoom);

            InvalidateSurface();

            OnMouseWheel(e);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            CacheManager?.ResetAll();
            CacheManager?.LoadBrushes(RenderContext);
            CacheManager?.LoadBitmaps(RenderContext);

            if (ViewManager?.Root != null)
                CacheManager?.Bind(ViewManager.Document);

            InvalidateVisual();
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            // quickfix b/c asychronous events mean we can't use Handled
            // which creates the problem of backspace registering as 
            // text input for some reason
            if (e.Text == "\b")
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

            if (ViewManager == null) return;

            using (CacheManager.Lock())
            {

                target.Transform(ViewManager.Transform, true);

                ViewManager.Render(target, CacheManager);

                ViewManager.Root.Render(target, CacheManager);

                if (SelectionManager == null) return;

                SelectionManager.Render(target, CacheManager);

                if (ToolManager?.Tool == null) return;

                ToolManager.Tool.Render(target, CacheManager);

                if (ToolManager.Tool.Cursor == null) return;

                target.Transform(
                    Matrix3x2.CreateRotation(ToolManager.Tool.CursorRotate, new Vector2(8)) *
                    Matrix3x2.CreateTranslation(_lastPosition - new Vector2(8)), true);

                target.DrawBitmap(ToolManager.Tool.Cursor);
            }
        }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_events.Count > 0)
                {
                    var evt = _events.Dequeue();

                    var pos = ViewManager.ToArtSpace(evt.Position);

                    switch (evt.Type)
                    {
                        case InputEventType.MouseDown:
                            if (!ToolManager.MouseDown(pos))
                                SelectionManager.MouseDown(pos);
                            break;
                        case InputEventType.MouseUp:
                            if (!ToolManager.MouseUp(pos))
                                SelectionManager.MouseUp(pos);
                            break;
                        case InputEventType.MouseMove:
                            if (Time.Now - evt.Time > 16
                            ) // at 16ms, begin skipping mouse moves
                                lock (_events)
                                {
                                    if (_events.Any() &&
                                        _events.Peek().Type == InputEventType.MouseMove)
                                        continue;
                                }

                            if (!ToolManager.MouseMove(pos))
                                SelectionManager.MouseMove(pos);
                            break;

                        case InputEventType.TextInput:
                            ToolManager.TextInput(evt.Text);
                            break;
                        case InputEventType.KeyUp:
                            ToolManager.KeyUp(evt.Key, evt.Modifier);
                            break;
                        case InputEventType.KeyDown:
                            ToolManager.KeyDown(evt.Key, evt.Modifier);
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
            CacheManager?.ResetAll();
            _eventLoop = false;
        }

        #region IArtContext Members

        public void InvalidateSurface() { base.InvalidateSurface(null); }

        public IBrushManager BrushManager { get; private set; }
        public ICacheManager CacheManager { get; private set; }
        public IHistoryManager HistoryManager { get; private set; }

        public ISelectionManager SelectionManager { get; private set; }
        public IToolManager ToolManager { get; private set; }
        public IViewManager ViewManager { get; private set; }

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