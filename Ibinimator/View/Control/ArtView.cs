using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System;

namespace Ibinimator.View.Control
{
    internal enum ArtViewHandle
    {
        TopLeft, Top, TopRight,
        Left, Translation, Right,
        BottomLeft, Bottom, BottomRight,
        Rotation
    }

    public class ArtView : D2DImage
    {

        #region Fields

        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.Register("Selection", typeof(IList<Model.Layer>), typeof(ArtView), new PropertyMetadata(OnSelectionChanged));

        internal ICacheManager CacheManager;
        internal ISelectionManager SelectionManager;
        internal IViewManager ViewManager;

        private Factory factory;
        private RenderTarget renderTarget;

        #endregion Fields

        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;

            SelectionManager = new SelectionManager(this);
            CacheManager = new CacheManager(this);
            ViewManager = new ViewManager(this);

            Unloaded += OnUnloaded;
        }

        #endregion Constructors

        #region Properties

        public IList<Model.Layer> Selection
        {
            get { return (IList<Model.Layer>)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public RenderTarget RenderTarget => renderTarget;

        #endregion Properties

        #region Methods

        public void InvalidateSurface(RectangleF? area)
        {
            if (area == null)
                base.InvalidateSurface(null);
            else
                base.InvalidateSurface(ViewManager.FromArtSpace(area.Value).Round());
        }

        protected override async void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            await Task.Run(() => SelectionManager.OnMouseUp(-Vector2.One, factory));
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionManager.OnMouseDown(pos, factory));
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionManager.OnMouseMove(pos, factory));

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
            var pos = ViewManager.ToArtSpace(new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionManager.OnMouseUp(pos, factory));
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            float scale = 1 + e.Delta / 500f;
            var pos1 = e.GetPosition(this);
            var pos = ViewManager.ToArtSpace(new Vector2((float)pos1.X, (float)pos1.Y));

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                ViewManager.Zoom *= scale;
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                ViewManager.Pan += new Vector2(e.Delta / 100f, 0);
            else
                ViewManager.Pan += new Vector2(0, e.Delta / 100f);

            InvalidateSurface(null);

            base.OnMouseWheel(e);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            factory = target.Factory;
            renderTarget = target;

            CacheManager.ResetAll();
            CacheManager.LoadBrushes(target);
            CacheManager.LoadBitmaps(target);

            if(ViewManager.Root != null)
                CacheManager.BindLayer(ViewManager.Root);
        }

        protected override void Render(RenderTarget target)
        {
            lock (SelectionManager)
            {
                target.Clear(Color4.White);

                target.Transform = ViewManager.Transform;

                ViewManager.Root.Render(target, CacheManager);

                SelectionManager.Render(target, CacheManager);
            }
        }

        private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var av = d as ArtView;
            var newLayer = e.NewValue as Model.Layer;
            var oldLayer = e.OldValue as Model.Layer;
            newLayer.PropertyChanged += av.OnRootPropertyChanged;
            if(oldLayer != null)
                oldLayer.PropertyChanged -= av.OnRootPropertyChanged;

            av.CacheManager.ResetLayerCache();
            av.CacheManager.BindLayer(newLayer);
            av.InvalidateSurface(null);
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            if (e.OldValue is INotifyCollectionChanged old)
                old.CollectionChanged -= av.OnSelectionUpdated;
            if (e.NewValue is INotifyCollectionChanged incc)
                incc.CollectionChanged += av.OnSelectionUpdated;

            av.SelectionManager.Selection = e.NewValue as IList<Model.Layer>;
            av.SelectionManager.Update(true);
        }

        private void OnRootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // this event fires whenever any of the layers changes anything, not just the root layer
            var layer = sender as Model.Layer;

            CacheManager.UpdateLayer(layer, e.PropertyName);
            SelectionManager.Update(false);
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