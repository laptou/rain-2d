using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage
    {
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);

        private readonly Stack<(long time, MouseEventType type, Vector2 position)> _events
            = new Stack<(long, MouseEventType, Vector2)>();

        private bool _eventLoop;
        private Factory _factory;
        private Vector2 _lastPosition;

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

        public RenderTarget RenderTarget { get; private set; }
        public ISelectionManager SelectionManager { get; private set; }
        public IToolManager ToolManager { get; private set; }
        public IViewManager ViewManager { get; private set; }

        public void InvalidateSurface()
        {
            base.InvalidateSurface(null);
        }

        public void SetManager<T>(T manager) where T : IArtViewManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            var managerInterfaces =
                typeof(T).FindInterfaces((type, criteria) =>
                        typeof(IArtViewManager).IsAssignableFrom(type), null)
                    .Concat(new[] {typeof(T)});

            var interfaces = managerInterfaces.ToList();

            if (interfaces.Contains(typeof(IBrushManager)))
                BrushManager = (IBrushManager) manager;

            if (interfaces.Contains(typeof(ICacheManager)))
            {
                CacheManager?.ResetAll();

                CacheManager = (ICacheManager) manager;

                if (RenderTarget != null)
                {
                    CacheManager.LoadBrushes(RenderTarget);
                    CacheManager.LoadBitmaps(RenderTarget);

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

                CacheManager?.ResetLayerCache();
                if (ViewManager?.Root != null)
                    CacheManager?.Bind(ViewManager.Document);
            }

            InvalidateSurface();
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

        protected override async void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (ToolManager != null)
                await Task.Run(() => ToolManager.KeyDown(e));
        }

        protected override async void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (ToolManager != null)
                await Task.Run(() => ToolManager.KeyUp(e));
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos = e.GetPosition(this);
            var vec = new Vector2((float) pos.X, (float) pos.Y);

            lock (_events)
            {
                _events.Push((DateTime.Now.Ticks, MouseEventType.Down, vec));
            }

            _eventFlag.Set();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos = e.GetPosition(this);
            var vec = new Vector2((float) pos.X, (float) pos.Y);

            lock (_events)
            {
                _events.Push((DateTime.Now.Ticks, MouseEventType.Move, vec));
            }

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
            {
                _events.Push((DateTime.Now.Ticks, MouseEventType.Up, vec));
            }

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
            RenderTarget = target;

            CacheManager?.ResetAll();
            CacheManager?.LoadBrushes(target);
            CacheManager?.LoadBitmaps(target);

            if (ViewManager?.Root != null)
                CacheManager?.Bind(ViewManager.Document);
        }

        protected override void Render(RenderTarget target)
        {
            target.Clear(Color4.White);

            if (ViewManager == null) return;

            target.Transform = ViewManager.Transform;

            ViewManager.Root.Render(target, CacheManager);

            if (SelectionManager == null) return;

            SelectionManager.Render(target, CacheManager);

            if (ToolManager?.Tool == null) return;

            ToolManager.Tool.Render(target, CacheManager);

            if (ToolManager.Tool.Cursor == null) return;

            target.Transform =
                Matrix3x2.Scaling(1f / 3) *
                Matrix3x2.Rotation(ToolManager.Tool.CursorRotate, new Vector2(8)) *
                Matrix3x2.Translation(_lastPosition - new Vector2(8));
            target.DrawBitmap(ToolManager.Tool.Cursor, 1, BitmapInterpolationMode.Linear);
        }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_eventLoop)
                {
                    (long time, MouseEventType type, Vector2 position) evt;

                    lock (_events)
                    {
                        if (_events.Count == 0) break;
                        evt = _events.Pop();
                    }

                    switch (evt.type)
                    {
                        case MouseEventType.Down:
                            ToolManager.MouseDown(ViewManager.ToArtSpace(evt.position));
                            break;
                        case MouseEventType.Up:
                            ToolManager.MouseUp(ViewManager.ToArtSpace(evt.position));
                            break;
                        case MouseEventType.Move:
                            ToolManager.MouseMove(ViewManager.ToArtSpace(evt.position));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Dispatcher.Invoke(() =>
                    Cursor = ToolManager?.Tool?.Cursor != null ? Cursors.None : Cursors.Arrow);

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