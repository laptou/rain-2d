using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.View.Control
{
    

    public class ArtView : D2DImage
    {
        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;
            Unloaded += OnUnloaded;
        }

        #endregion Constructors

        #region Fields

        public IBrushManager BrushManager { get; private set; }
        public IToolManager ToolManager { get; private set; }
        public ICacheManager CacheManager { get; private set; }
        public ISelectionManager SelectionManager { get; private set; }
        public IViewManager ViewManager { get; private set; }

        private Factory _factory;

        #endregion Fields

        #region Properties

        public RenderTarget RenderTarget { get; private set; }

        #endregion Properties

        #region Methods

        public void InvalidateSurface()
        {
            base.InvalidateSurface(null);
        }

        protected override async void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (ToolManager != null)
                await Task.Run(() => ToolManager.MouseUp(-Vector2.One));
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            if (ViewManager == null) return;

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            if (ToolManager != null)
                await Task.Run(() => ToolManager.MouseDown(pos));
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (ViewManager == null) return;

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            if(ToolManager != null)
                await Task.Run(() => ToolManager.MouseMove(pos));

            Cursor = SelectionManager?.Cursor != null ? Cursors.None : Cursors.Arrow;
        }

        protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            if (ViewManager == null) return;

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            if(ToolManager != null)
                await Task.Run(() => ToolManager.MouseUp(pos));
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
                ViewManager.Pan += new Vector2(e.Delta / 100f, 0);
            else
                ViewManager.Pan += new Vector2(0, e.Delta / 100f);

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
                CacheManager?.BindLayer(ViewManager.Root);
        }

        protected override void Render(RenderTarget target)
        {
            target.Clear(Color4.White);

            target.Transform = ViewManager?.Transform ?? Matrix.Identity;

            ViewManager?.Root?.Render(target, CacheManager);

            SelectionManager?.Render(target, CacheManager);

            ToolManager?.Tool?.Render(target, CacheManager);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CacheManager?.ResetAll();
        }

        public void SetManager<T>(T manager) where T : IArtViewManager
        {
            if(manager == null) throw new ArgumentNullException(nameof(manager));

            var managerInterfaces =
                typeof(T).FindInterfaces((type, criteria) =>
                    typeof(IArtViewManager).IsAssignableFrom(type), null)
                    .Concat(new []{ typeof(T) });

            var interfaces = managerInterfaces.ToList();

            if (interfaces.Contains(typeof(IBrushManager)))
            {
                BrushManager = (IBrushManager)manager;
            }

            if (interfaces.Contains(typeof(ICacheManager)))
            {
                CacheManager?.ResetAll();

                CacheManager = (ICacheManager)manager;

                if (RenderTarget != null)
                {
                    CacheManager.LoadBrushes(RenderTarget);
                    CacheManager.LoadBitmaps(RenderTarget);

                    if (ViewManager?.Root != null)
                        CacheManager.BindLayer(ViewManager.Root);
                }
            }

            if (interfaces.Contains(typeof(ISelectionManager)))
            {
                SelectionManager = (ISelectionManager)manager;
            }

            if (interfaces.Contains(typeof(IToolManager)))
            {
                ToolManager = (IToolManager)manager;
            }

            if (interfaces.Contains(typeof(IViewManager)))
            {
                ViewManager = (IViewManager)manager;

                CacheManager?.ResetLayerCache();
                if (ViewManager?.Root != null)
                    CacheManager?.BindLayer(ViewManager.Root);
            }

            InvalidateSurface();
        }

        #endregion Methods
    }
}