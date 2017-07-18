using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Diagnostics;

namespace Ibinimator.View.Control
{
    internal enum ArtViewHandle
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
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

        internal CacheHelper cacheHelper;
        internal SelectionHelper selectionHelper;
        private Factory factory;
        private RenderTarget renderTarget;
        private Matrix3x2 viewTransform = Matrix3x2.Identity;

        #endregion Fields

        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;

            selectionHelper = new SelectionHelper(this);
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

        #endregion Properties

        #region Methods

        public void InvalidateSurface(RectangleF? area)
        {
            Dispatcher.Invoke(() =>
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
            });
        }

        protected override async void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            await Task.Run(() => selectionHelper.OnMouseUp(-Vector2.One, factory));
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            CaptureMouse();

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => selectionHelper.OnMouseDown(pos, factory));
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => selectionHelper.OnMouseMove(pos, factory));
        }

        protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            ReleaseMouseCapture();

            var pos1 = e.GetPosition(this);
            var pos = Matrix3x2.TransformPoint(Matrix3x2.Invert(viewTransform), new Vector2((float)pos1.X, (float)pos1.Y));

            await Task.Run(() => selectionHelper.OnMouseUp(pos, factory));
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

            cacheHelper = new CacheHelper(this);

            cacheHelper.LoadBrushes(target);

            cacheHelper.BindLayer(Root);
        }

        protected override void Render(RenderTarget target)
        {
            target.Clear(Color4.White);

            target.Transform = viewTransform;

            Root.Render(target, cacheHelper);

            selectionHelper.Render(target, cacheHelper.GetBrush("A1"), cacheHelper.GetBrush("L1"), 1f / viewTransform.ScaleVector.X);
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

            av.selectionHelper.Selection = e.NewValue as IList<Model.Layer>;
            av.selectionHelper.UpdateSelection();
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

            cacheHelper.UpdateLayer(layer, e.PropertyName);
            selectionHelper.UpdateSelection();
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            selectionHelper.UpdateSelection();
        }

        #endregion Methods

    }

    public class CacheHelper : Model.Model
    {
        #region Fields

        private Dictionary<Model.Layer, RectangleF> bounds = new Dictionary<Model.Layer, RectangleF>();
        private Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();
        private Dictionary<Model.Layer, Brush> fills = new Dictionary<Model.Layer, Brush>();
        private Dictionary<Model.Layer, TransformedGeometry> geometries = new Dictionary<Model.Layer, TransformedGeometry>();

        private Dictionary<Model.Layer, (Brush brush, float width, StrokeStyle style)> strokes =
                            new Dictionary<Model.Layer, (Brush, float, StrokeStyle)>();

        #endregion Fields

        #region Constructors

        public CacheHelper(ArtView artView)
        {
            ArtView = artView;
        }

        #endregion Constructors

        #region Properties

        public ArtView ArtView { get; set; }

        #endregion Properties

        #region Methods

        public Brush BindBrush(Model.Shape shape, Model.BrushInfo brush)
        {
            if (brush == null) return null;

            RenderTarget target = ArtView.RenderTarget;

            Brush fill = brush.ToDirectX(target);

            switch (brush.BrushType)
            {
                case Model.BrushType.Color:
                    brush.PropertyChanged += (s, e) =>
                    {
                        var bi = s as Model.BrushInfo;

                        switch (e.PropertyName)
                        {
                            case "Opacity":
                                fill.Opacity = brush.Opacity;
                                break;

                            case "Color":
                                ((SolidColorBrush)fill).Color = bi.Color;
                                break;
                        }

                        InvalidateLayer(shape);
                    };
                    break;

                case Model.BrushType.LinearGradient:
                    break;

                case Model.BrushType.RadialGradient:
                    break;
            }

            return fill;
        }

        public void BindLayer(Model.Layer layer)
        {
            RenderTarget target = ArtView.RenderTarget;

            if (layer is Model.Shape shape)
            {
                if (shape.FillBrush != null)
                    fills[shape] = BindBrush(shape, shape.FillBrush);
                if (shape.StrokeBrush != null)
                    strokes[shape] =
                        (BindBrush(shape, shape.StrokeBrush), shape.StrokeWidth, new StrokeStyle(target.Factory, shape.StrokeStyle));
            }

            foreach (var subLayer in layer.SubLayers)
                BindLayer(subLayer);
        }

        public RectangleF GetBounds(Model.Layer layer) =>
            bounds.ContainsKey(layer) ? bounds[layer] : bounds[layer] = layer.GetTransformedBounds();

        public Brush GetBrush(string key) => brushes[key];

        public Brush GetFill(Model.Shape layer) =>
            fills.ContainsKey(layer) ? fills[layer] : fills[layer] = layer.FillBrush.ToDirectX(ArtView.RenderTarget);

        public TransformedGeometry GetGeometry(Model.Shape layer) =>
            geometries.ContainsKey(layer) ? geometries[layer] : geometries[layer] = layer.GetTransformedGeometry(ArtView.RenderTarget.Factory);

        public (Brush brush, float width, StrokeStyle style) GetStroke(Model.Shape layer, RenderTarget target) =>
            strokes.ContainsKey(layer) ? strokes[layer] : strokes[layer] = (
                    layer.StrokeBrush.ToDirectX(target),
                    layer.StrokeWidth,
                    new StrokeStyle(target.Factory, layer.StrokeStyle));

        public void LoadBrushes(RenderTarget target)
        {
            foreach (DictionaryEntry entry in Application.Current.Resources)
            {
                if (entry.Value is System.Windows.Media.Color color)
                {
                    brushes[entry.Key as string] =
                        new SolidColorBrush(
                            target,
                            new RawColor4(
                                color.R / 255f,
                                color.G / 255f,
                                color.B / 255f,
                                color.A / 255f));
                }
            }
        }

        public void UpdateLayer(Model.Layer layer, string property, bool? bubble = null)
        {
            Model.Shape shape = layer as Model.Shape;

            switch (property)
            {
                case nameof(Model.Layer.Transform):
                    bounds[layer] = layer.GetTransformedBounds();

                    geometries.TryGetValue(layer, out TransformedGeometry geometry);
                    geometry?.Dispose();

                    if (shape != null)
                        geometries[layer] = shape.GetTransformedGeometry(ArtView.RenderTarget.Factory);

                    if (bubble != false && layer.Parent != null)
                        UpdateLayer(layer.Parent, property, true);

                    if(bubble != true)
                        foreach (var subLayer in layer.SubLayers)
                            UpdateLayer(subLayer, property, false);

                    InvalidateLayer(layer);
                    break;

                case nameof(Model.Shape.FillBrush):
                    fills.TryGetValue(layer, out Brush fill);
                    fill?.Dispose();
                    fills[layer] = shape.FillBrush?.ToDirectX(ArtView.RenderTarget);
                    InvalidateLayer(layer);
                    break;

                case nameof(Model.Shape.StrokeBrush):
                    strokes.TryGetValue(layer, out (Brush brush, float, StrokeStyle) stroke);
                    stroke.brush?.Dispose();
                    stroke.brush = shape.StrokeBrush?.ToDirectX(ArtView.RenderTarget);
                    InvalidateLayer(layer);
                    break;

                default:
                    break;
            }
        }

        private void InvalidateLayer(Model.Layer layer)
        {
            ArtView.InvalidateSurface(GetBounds(layer));
        }

        #endregion Methods

    }

    internal class SelectionHelper : Model.Model
    {
        #region Fields

        public RectangleF SelectionBounds;
        public RectangleF SelectionBox;
        public Matrix3x2 SelectionTransform;
        private Vector2? lastPosition;
        private bool moved;
        private bool selecting;
        private object sync = new object();

        #endregion Fields

        #region Constructors

        public SelectionHelper(ArtView artView)
        {
            ArtView = artView;
        }

        #endregion Constructors

        #region Properties

        public ArtView ArtView { get; set; }
        public Cursor Cursor
        {
            get => ArtView?.Dispatcher.Invoke(() => ArtView.Cursor);
            set => ArtView?.Dispatcher.Invoke(() => ArtView.Cursor = value);
        }
        public ArtViewHandle? ResizingHandle { get; set; }
        public Model.Layer Root => ArtView?.Dispatcher.Invoke(() => ArtView.Root);
        public IList<Model.Layer> Selection { get; set; }

        #endregion Properties

        #region Methods

        private void UI(Action a) => ArtView.Dispatcher.Invoke(a);
        private T UI<T>(Func<T> a) => ArtView.Dispatcher.Invoke<T>(a);

        public void ClearSelection()
        {
            while (Selection.Count > 0)
                Selection[0].Selected = false;
        }

        public void OnMouseDown(Vector2 pos, Factory factory)
        {
            lock (sync)
            {
                moved = false;

                if (Selection.Count > 0)
                {
                    List<(Vector2 pos, ArtViewHandle handle)> handles = new List<(Vector2, ArtViewHandle)>();

                    float x1 = SelectionBounds.Left, y1 = SelectionBounds.Top,
                        x2 = SelectionBounds.Right, y2 = SelectionBounds.Bottom;

                    handles.Add((new Vector2(x1, y1), ArtViewHandle.TopLeft));
                    handles.Add((new Vector2(x2, y1), ArtViewHandle.TopRight));
                    handles.Add((new Vector2(x2, y2), ArtViewHandle.BottomRight));
                    handles.Add((new Vector2(x1, y2), ArtViewHandle.BottomLeft));
                    handles.Add((new Vector2((x1 + x2) / 2, y1), ArtViewHandle.Top));
                    handles.Add((new Vector2(x1, (y1 + y2) / 2), ArtViewHandle.Left));
                    handles.Add((new Vector2(x2, (y1 + y2) / 2), ArtViewHandle.Right));
                    handles.Add((new Vector2((x1 + x2) / 2, y2), ArtViewHandle.Bottom));

                    bool hit = false;

                    foreach (var h in handles)
                    {
                        if ((h.pos.X - pos.X) * (h.pos.X - pos.X) +
                            (h.pos.Y - pos.Y) * (h.pos.Y - pos.Y) < 6.25)
                        {
                            ResizingHandle = h.handle;
                            hit = true;
                            break;
                        }
                    }

                    if (!hit)
                    {
                        foreach (var l in Selection)
                        {
                            if (l.Hit(factory, pos, l.Parent.AbsoluteTransform) != null)
                            {
                                ResizingHandle = ArtViewHandle.Center;
                                hit = true;
                                break;
                            }
                        }
                    }

                    if (!hit && !ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers).HasFlag(ModifierKeys.Shift))
                        ClearSelection();
                }

                if (Selection.Count == 0 && !selecting)
                {
                    SelectionBox = new RectangleF(pos.X, pos.Y, 0, 0);
                    selecting = true;
                }

                lastPosition = pos;

                UpdateSelection();
            }
        }

        public void OnMouseMove(Vector2 pos, Factory factory)
        {
            lock (sync)
            {
                moved = true;

                lastPosition = lastPosition ?? pos;

                float width = Math.Max(1f, SelectionBounds.Width),
                    height = Math.Max(1f, SelectionBounds.Height);

                if (ResizingHandle != null)
                {
                    switch (ResizingHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    -(pos.X - SelectionBounds.Right) / width,
                                    -(pos.Y - SelectionBounds.Bottom) / height,
                                    SelectionBounds.BottomRight);
                            break;

                        case ArtViewHandle.Top:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    1,
                                    -(pos.Y - SelectionBounds.Bottom) / height,
                                    new Vector2(SelectionBounds.Center.X, SelectionBounds.Bottom));
                            break;

                        case ArtViewHandle.TopRight:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    (pos.X - SelectionBounds.Left) / width,
                                    -(pos.Y - SelectionBounds.Bottom) / height,
                                    SelectionBounds.BottomLeft);
                            break;

                        case ArtViewHandle.Left:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    -(pos.X - SelectionBounds.Left) / width,
                                    1,
                                    new Vector2(SelectionBounds.Right, SelectionBounds.Center.Y));
                            break;

                        case ArtViewHandle.Center:
                            SelectionTransform = Matrix3x2.Translation(pos - lastPosition.Value);
                            break;

                        case ArtViewHandle.Right:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    (pos.X - SelectionBounds.Left) / width,
                                    1,
                                    new Vector2(SelectionBounds.Left, SelectionBounds.Center.Y));
                            break;

                        case ArtViewHandle.BottomLeft:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    -(pos.X - SelectionBounds.Right) / width,
                                    (pos.Y - SelectionBounds.Top) / height,
                                    SelectionBounds.TopRight);
                            break;

                        case ArtViewHandle.Bottom:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    1,
                                    (pos.Y - SelectionBounds.Top) / height,
                                    new Vector2(SelectionBounds.Center.X, SelectionBounds.Top));
                            break;

                        case ArtViewHandle.BottomRight:
                            SelectionTransform =
                                Matrix3x2.Scaling(
                                    (pos.X - SelectionBounds.Left) / width,
                                    (pos.Y - SelectionBounds.Top) / height,
                                    SelectionBounds.TopLeft);
                            break;
                    }

                    if (SelectionTransform.ScaleVector.X < 0)
                    {
                        switch (ResizingHandle)
                        {
                            case ArtViewHandle.TopLeft:
                                ResizingHandle = ArtViewHandle.TopRight;
                                break;

                            case ArtViewHandle.TopRight:
                                ResizingHandle = ArtViewHandle.TopLeft;
                                break;

                            case ArtViewHandle.Left:
                                ResizingHandle = ArtViewHandle.Right;
                                break;

                            case ArtViewHandle.Right:
                                ResizingHandle = ArtViewHandle.Left;
                                break;

                            case ArtViewHandle.BottomLeft:
                                ResizingHandle = ArtViewHandle.BottomRight;
                                break;

                            case ArtViewHandle.BottomRight:
                                ResizingHandle = ArtViewHandle.BottomLeft;
                                break;
                        }
                    }

                    if (SelectionTransform.ScaleVector.Y < 0)
                    {
                        switch (ResizingHandle)
                        {
                            case ArtViewHandle.TopLeft:
                                ResizingHandle = ArtViewHandle.BottomLeft;
                                break;

                            case ArtViewHandle.TopRight:
                                ResizingHandle = ArtViewHandle.BottomRight;
                                break;

                            case ArtViewHandle.Top:
                                ResizingHandle = ArtViewHandle.Bottom;
                                break;

                            case ArtViewHandle.Bottom:
                                ResizingHandle = ArtViewHandle.Top;
                                break;

                            case ArtViewHandle.BottomLeft:
                                ResizingHandle = ArtViewHandle.TopLeft;
                                break;

                            case ArtViewHandle.BottomRight:
                                ResizingHandle = ArtViewHandle.TopRight;
                                break;
                        }
                    }

                    SelectionTransform.M11 = MathUtils.AbsMax(0.001f, SelectionTransform.M11);
                    SelectionTransform.M22 = MathUtils.AbsMax(0.001f, SelectionTransform.M22);

                    foreach (var layer in Selection)
                    {
                        layer.Transform *= SelectionTransform;
                    }

                    SelectionTransform = Matrix.Identity;

                    UpdateSelection();
                }

                if (Selection.Count > 0)
                {
                    List<(Vector2 pos, Cursor cur)> handles = new List<(Vector2, Cursor)>();

                    Vector2 tl = SelectionBounds.TopLeft,
                        br = SelectionBounds.BottomRight;

                    float x1 = tl.X, y1 = tl.Y,
                        x2 = br.X, y2 = br.Y;

                    handles.Add((new Vector2(x1, y1), Cursors.SizeNWSE));
                    handles.Add((new Vector2(x2, y1), Cursors.SizeNESW));
                    handles.Add((new Vector2(x2, y2), Cursors.SizeNWSE));
                    handles.Add((new Vector2(x1, y2), Cursors.SizeNESW));
                    handles.Add((new Vector2((x1 + x2) / 2, y1), Cursors.SizeNS));
                    handles.Add((new Vector2(x1, (y1 + y2) / 2), Cursors.SizeWE));
                    handles.Add((new Vector2(x2, (y1 + y2) / 2), Cursors.SizeWE));
                    handles.Add((new Vector2((x1 + x2) / 2, y2), Cursors.SizeNS));

                    foreach (var h in handles)
                    {
                        if ((h.pos.X - pos.X) * (h.pos.X - pos.X) +
                            (h.pos.Y - pos.Y) * (h.pos.Y - pos.Y) < 6.25)
                        {
                            Cursor = h.cur;
                            break;
                        }
                        else Cursor = Cursors.Arrow;
                    }
                }

                if (selecting && Selection.Count == 0)
                {
                    ArtView.InvalidateSurface(SelectionBox.Inflate(2));

                    if (pos.X < SelectionBox.Left)
                        SelectionBox.Left = pos.X;
                    else
                        SelectionBox.Right = pos.X;

                    if (pos.Y < SelectionBox.Top)
                        SelectionBox.Top = pos.Y;
                    else
                        SelectionBox.Bottom = pos.Y;

                    ArtView.InvalidateSurface(SelectionBox.Inflate(2));
                }

                lastPosition = pos;
            }
        }

        public void OnMouseUp(Vector2 pos, Factory factory)
        {
            lock (sync)
            {
                ResizingHandle = null;
                SelectionTransform = Matrix3x2.Identity;

                if (!moved)
                {
                    var hit = Root.Hit(factory, pos, Matrix3x2.Identity);

                    if (!UI(() => Keyboard.Modifiers).HasFlag(ModifierKeys.Shift))
                        ClearSelection();

                    if (hit != null)
                        hit.Selected = true;
                }

                if (selecting)
                {
                    Parallel.ForEach(Root.Flatten(), layer =>
                    {
                        var bounds = ArtView.cacheHelper.GetBounds(layer);
                        SelectionBox.Contains(ref bounds, out bool contains);
                        layer.Selected = layer.Selected || contains;
                    });

                    ArtView.InvalidateSurface(SelectionBox.Inflate(2));
                    SelectionBox = RectangleF.Empty;

                    selecting = false;
                }

                UpdateSelection();
            }
        }

        public void Render(RenderTarget target, Brush fill, Brush stroke, float strokeWidth)
        {
            void RenderHandles(RectangleF rect)
            {
                // draw handles
                List<Vector2> handles = new List<Vector2>();

                float x1 = rect.Left, y1 = rect.Top,
                    x2 = rect.Right, y2 = rect.Bottom;

                handles.Add(new Vector2(x1, y1));
                handles.Add(new Vector2(x2, y1));
                handles.Add(new Vector2(x2, y2));
                handles.Add(new Vector2(x1, y2));
                handles.Add(new Vector2((x1 + x2) / 2, y1));
                handles.Add(new Vector2(x1, (y1 + y2) / 2));
                handles.Add(new Vector2(x2, (y1 + y2) / 2));
                handles.Add(new Vector2((x1 + x2) / 2, y2));

                foreach (Vector2 v in handles)
                {
                    Ellipse e = new Ellipse(v, 5f / target.Transform.M11, 5f / target.Transform.M22);
                    target.FillEllipse(e, fill);
                    target.DrawEllipse(e, stroke, 2f / target.Transform.M11);
                }
            }
            
            if (Selection.Count == 1)
            {
                var layer = Selection[0];

                RectangleF rect = ArtView.cacheHelper.GetBounds(layer);

                using (RectangleGeometry rg = new RectangleGeometry(target.Factory, rect))
                    using(TransformedGeometry tg = new TransformedGeometry(target.Factory, rg, layer.Parent.AbsoluteTransform))
                        target.DrawGeometry(tg, fill, 1f / target.Transform.M11);

                if (layer is Model.Shape shape)
                {
                    target.Transform *= shape.Parent.AbsoluteTransform;
                    target.DrawGeometry(ArtView.cacheHelper.GetGeometry(shape), fill, 1f / target.Transform.M11);
                    target.Transform *= Matrix3x2.Invert(shape.Parent.AbsoluteTransform);
                }

                RenderHandles(rect);
            }
            else if (Selection.Count > 1)
            {
                // draw selection outline
                target.DrawRectangle(SelectionBounds, fill, strokeWidth);

                foreach (var layer in Selection)
                    if (layer is Model.Shape shape)
                    {
                        target.Transform *= shape.Parent.AbsoluteTransform;
                        target.DrawGeometry(ArtView.cacheHelper.GetGeometry(shape), fill, 1f / target.Transform.M11);
                        target.Transform *= Matrix3x2.Invert(shape.Parent.AbsoluteTransform);
                    }

                RenderHandles(SelectionBounds);
            }

            if (!SelectionBox.IsEmpty)
                {
                    target.DrawRectangle(SelectionBox, fill, strokeWidth);
                    fill.Opacity = 0.25f;
                    target.FillRectangle(SelectionBox, fill);
                    fill.Opacity = 1.0f;
                }
        }

        public void UpdateSelection()
        {   
            InvalidateSurface();

            if (Selection.Count == 0)
            {
                SelectionBounds = RectangleF.Empty;
                return;
            }

            Model.Layer first = Selection.FirstOrDefault();
            (float x1, float y1, float x2, float y2) = (first?.X ?? 0, first?.Y ?? 0, first?.X ?? 0, first?.Y ?? 0);

            Parallel.ForEach(Selection, l =>
            {
                var b = MathUtils.Bounds(l.GetBounds(), l.AbsoluteTransform);

                if (b.Left < x1) x1 = b.Left;
                if (b.Top < y1) y1 = b.Top;
                if (b.Right > x2) x2 = b.Right;
                if (b.Bottom > y2) y2 = b.Bottom;
            });

            SelectionBounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

            InvalidateSurface();
        }

        private void InvalidateSurface()
        {
            ArtView.InvalidateSurface(SelectionBounds.Inflate(7));
        }

        #endregion Methods

    }
}