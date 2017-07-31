using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.View.Control
{
    internal enum ArtViewHandle
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Translation,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        Rotation
    }

    public class ArtView : D2DImage
    {
        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;

            ToolManager = new ToolManager(this);
            SelectionManager = new SelectionManager(this);
            CacheManager = new CacheManager(this);
            ViewManager = new ViewManager(this);

            Unloaded += OnUnloaded;
        }

        #endregion Constructors

        #region Fields

        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.Register("Selection", typeof(IList<Layer>), typeof(ArtView),
                new PropertyMetadata(OnSelectionChanged));

        internal IBrushManager BrushManager;
        internal IToolManager ToolManager;
        internal ICacheManager CacheManager;
        internal ISelectionManager SelectionManager;
        internal IViewManager ViewManager;

        private Factory _factory;

        #endregion Fields

        #region Properties

        public IList<Layer> Selection
        {
            get => (IList<Layer>) GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

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

            await Task.Run(() => ToolManager.MouseUp(-Vector2.One));
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            await Task.Run(() => ToolManager.MouseDown(pos));
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            await Task.Run(() => ToolManager.MouseMove(pos));

            if (SelectionManager.Cursor != null)
                Cursor = Cursors.None;
            else
                Cursor = Cursors.Arrow;
        }

        protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float) pos1.X, (float) pos1.Y));

            await Task.Run(() => ToolManager.MouseUp(pos));
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
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

            CacheManager.ResetAll();
            CacheManager.LoadBrushes(target);
            CacheManager.LoadBitmaps(target);

            if (ViewManager.Root != null)
                CacheManager.BindLayer(ViewManager.Root);
        }

        protected override void Render(RenderTarget target)
        {
            target.Clear(Color4.White);

            target.Transform = ViewManager.Transform;

            ViewManager.Root.Render(target, CacheManager);

            SelectionManager.Render(target, CacheManager);

            ToolManager.Tool?.Render(target, CacheManager);
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var av = (ArtView) d;
            if (e.OldValue is INotifyCollectionChanged old)
                old.CollectionChanged -= av.OnSelectionUpdated;
            if (e.NewValue is INotifyCollectionChanged incc)
                incc.CollectionChanged += av.OnSelectionUpdated;

            av.SelectionManager.Selection = e.NewValue as IList<Layer>;
            av.SelectionManager.Update(true);
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectionManager.Update(true);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CacheManager.ResetAll();
        }

        #endregion Methods
    }
}