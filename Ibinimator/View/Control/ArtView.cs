using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service;
using Ibinimator.Service.Tools;
using SharpDX.Direct2D1;
using System.Numerics;
using Ibinimator.Core;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage, IArtContext
    {
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);

        private readonly Queue<(long time, MouseEventType type, Vector2 position)> _events
            = new Queue<(long, MouseEventType, Vector2)>();

        private bool _eventLoop;
        private Factory _factory;
        private Vector2 _lastPosition;
        private long _lastFrame;
        private float _lastFps;

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            Focusable = true;

            RenderTargetBound += OnRenderTargetBound;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public IBrushManager BrushManager { get; private set; }
        public ICacheManager CacheManager { get; private set; }
        public IHistoryManager HistoryManager { get; private set; }

        public ISelectionManager SelectionManager { get; private set; }
        public IToolManager ToolManager { get; private set; }
        public IViewManager ViewManager { get; private set; }

        public void InvalidateSurface()
        {
            base.InvalidateSurface(null);
        }

        public void SetManager<T>(T manager) where T : IArtContextManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            var managerInterfaces =
                typeof(T).FindInterfaces((type, criteria) =>
                        typeof(IArtContextManager).IsAssignableFrom(type), null)
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
                ViewManager.DocumentUpdated += (s, e) => CacheManager?.Bind(ViewManager.Document);

                CacheManager?.ResetDeviceResources();
                if (ViewManager?.Root != null)
                    CacheManager?.Bind(ViewManager.Document);
            }

            InvalidateSurface();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            if (e.LeftButton == MouseButtonState.Pressed)
                lock (_events)
                    _events.Enqueue((Time.Now, MouseEventType.Up, Vector2.Zero));
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

            e.Handled = ToolManager?.KeyDown(e) == true;
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            e.Handled = ToolManager?.KeyUp(e) == true;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos = e.GetPosition(this);
            var vec = new Vector2((float) pos.X, (float) pos.Y);

            lock (_events)
                _events.Enqueue((Time.Now, MouseEventType.Down, vec));

            _eventFlag.Set();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos = e.GetPosition(this);
            var vec = new Vector2((float) pos.X, (float) pos.Y);

            lock (_events)
                _events.Enqueue((Time.Now, MouseEventType.Move, vec));

            _eventFlag.Set();

            _lastPosition = vec;
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            var pos = e.GetPosition(this);
            var vec = new Vector2((float) pos.X, (float) pos.Y);

            lock (_events)
                _events.Enqueue((Time.Now, MouseEventType.Up, vec));

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
            _factory = target.Factory;

            CacheManager?.ResetAll();
            CacheManager?.LoadBrushes(RenderContext);
            CacheManager?.LoadBitmaps(RenderContext);

            if (ViewManager?.Root != null)
                CacheManager?.Bind(ViewManager.Document);
        }

        protected override void Render(RenderContext target)
        {
            target.Clear(new Color(0.5f));

            if (ViewManager == null) return;

            target.Transform(ViewManager.Transform, true);

            ViewManager.Render(target, CacheManager);

            ViewManager.Root.Render(target, CacheManager);

            if (SelectionManager == null) return;

            SelectionManager.Render(target, CacheManager);

            if (ToolManager?.Tool == null) return;

            ToolManager.Tool.Render(target, CacheManager);

            if (ToolManager.Tool.Cursor == null) return;

            target.Transform(
                Matrix3x2.CreateScale(1f / 3) *
                Matrix3x2.CreateRotation(ToolManager.Tool.CursorRotate, new Vector2(8)) *
                Matrix3x2.CreateTranslation(_lastPosition - new Vector2(8)), true);

            // target.DrawBitmap(ToolManager.Tool.Cursor, 1, BitmapInterpolationMode.Linear);

        }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_eventLoop)
                {
                    (long Time, MouseEventType Type, Vector2 position) evt;

                    lock (_events)
                    {
                        if (_events.Count == 0) break;
                        evt = _events.Dequeue();
                    }

                    var pos = ViewManager.ToArtSpace(evt.position);

                    switch (evt.Type)
                    {
                        case MouseEventType.Down:
                            if (!ToolManager.MouseDown(pos))
                                SelectionManager.MouseDown(pos);
                            break;
                        case MouseEventType.Up:
                            if (!ToolManager.MouseUp(pos))
                                SelectionManager.MouseUp(pos);
                            break;
                        case MouseEventType.Move:
                            if (Time.Now - evt.Time > 16) // at 16ms, begin skipping mouse moves
                                lock (_events)
                                    if(_events.Any() && _events.Peek().type == MouseEventType.Move)
                                        continue;

                            if (!ToolManager.MouseMove(pos))
                                SelectionManager.MouseMove(pos);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Dispatcher.Invoke(() => Cursor = ToolManager?.Tool?.Cursor != null ? Cursors.None : Cursors.Arrow);

                _eventFlag.WaitOne(1000);
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
    }

    public enum MouseEventType
    {
        Down,
        Up,
        Move
    }
}