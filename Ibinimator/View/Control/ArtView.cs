﻿using System.Threading.Tasks;
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

        public static readonly DependencyProperty RootProperty =
            DependencyProperty.Register("Root", typeof(Model.Layer), typeof(ArtView), new PropertyMetadata(null, OnRootChanged));

        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.Register("Selection", typeof(IEnumerable<Model.Layer>), typeof(ArtView), new PropertyMetadata(OnSelectionChanged));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(float), typeof(ArtView), new PropertyMetadata(1f, OnZoomChanged));

        internal CacheHelper Cache;
        internal SelectionHelper SelectionHelper;
        private Factory factory;
        private RenderTarget renderTarget;
        private Matrix3x2 viewTransform = Matrix3x2.Identity;

        #endregion Fields

        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;

            SelectionHelper = new SelectionHelper(this);
            Cache = new CacheHelper(this);

            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Cache.Reset();
        }

        #endregion Constructors

        #region Properties

        public Model.Layer Root
        {
            get { return (Model.Layer)GetValue(RootProperty); }
            set { SetValue(RootProperty, value); }
        }

        [Bindable(true)]
        public IList<Model.Layer> Selection
        {
            get { return (IList<Model.Layer>)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public float Zoom
        {
            get { return (float)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        internal RenderTarget RenderTarget => renderTarget;

        internal Matrix3x2 ViewTransform => viewTransform;

        #endregion Properties

        #region Methods

        public void InvalidateSurface(RectangleF? area)
        {
            if (area == null)
                base.InvalidateSurface(null);
            else
            {
                Rectangle rect = area.Value.Round();
                rect.X = (int)(rect.X * viewTransform.ScaleVector.X + viewTransform.TranslationVector.X);
                rect.Y = (int)(rect.Y * viewTransform.ScaleVector.Y + viewTransform.TranslationVector.Y);
                rect.Width = (int)(rect.Width * viewTransform.ScaleVector.X);
                rect.Height = (int)(rect.Height * viewTransform.ScaleVector.Y);

                base.InvalidateSurface(rect);
            }
        }

        protected override async void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            await Task.Run(() => SelectionHelper.OnMouseUp(-Vector2.One, factory));
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionHelper.OnMouseDown(pos, factory));
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionHelper.OnMouseMove(pos, factory));
        }

        protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => SelectionHelper.OnMouseUp(pos, factory));
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            float scale = 1 + e.Delta / 500f;
            var pos1 = e.GetPosition(this);
            var pos = (new Vector2((float)pos1.X, (float)pos1.Y) - viewTransform.TranslationVector) / viewTransform.ScaleVector;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                viewTransform *= Matrix3x2.Scaling(scale, scale, pos);

            InvalidateSurface(null);

            base.OnMouseWheel(e);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            factory = target.Factory;
            renderTarget = target;

            Cache.Reset();
            Cache.LoadBrushes(target);
            Cache.LoadBitmaps(target);
            Cache.BindLayer(Root);
        }

        protected override void Render(RenderTarget target)
        {
            lock (SelectionHelper)
            {
                target.Clear(Color4.White);

                target.Transform = viewTransform;

                Dispatcher.Invoke(() => Root).Render(target, Cache);

                SelectionHelper.Render(target, Cache.GetBrush("A1"), Cache.GetBrush("L1"));
            }
        }

        private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var av = d as ArtView;
            av.Root.PropertyChanged += av.OnRootPropertyChanged;
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            if (e.OldValue is INotifyCollectionChanged old)
                old.CollectionChanged -= av.OnSelectionUpdated;
            if (e.NewValue is INotifyCollectionChanged incc)
                incc.CollectionChanged += av.OnSelectionUpdated;

            av.SelectionHelper.Selection = e.NewValue as IList<Model.Layer>;
            av.SelectionHelper.UpdateSelection(true);
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            av.viewTransform.ScaleVector = new Vector2(av.Zoom);
        }

        private void OnRootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // this event fires whenever any of the layers changes anything, not just the root layer
            var layer = sender as Model.Layer;

            Cache.UpdateLayer(layer, e.PropertyName);
            SelectionHelper.UpdateSelection(false);
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectionHelper.UpdateSelection(true);
        }

        #endregion Methods
    }
}