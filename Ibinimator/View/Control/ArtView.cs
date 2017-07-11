using System.Threading.Tasks;
using SharpDX;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Shared;
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

        private Dictionary<Model.Layer, RectangleF> bounds = new Dictionary<Model.Layer, RectangleF>();

        private Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();
        private ArtViewHandle? currentHandle;
        private Factory factory;
        private RenderTarget renderTarget;

        private Dictionary<Model.Layer, Brush> fills = new Dictionary<Model.Layer, Brush>();
        private Dictionary<Model.Layer, Geometry> geometries = new Dictionary<Model.Layer, Geometry>();
        private Vector2? lastPosition;
        private bool moved;
        private RectangleF selectionBounds = new RectangleF();
        private Matrix3x2 selectionTransform = Matrix3x2.Identity;
        private object renderLock = new object();

        private Dictionary<Model.Layer, (Brush brush, float width, StrokeStyle style)> strokes =
                    new Dictionary<Model.Layer, (Brush, float, StrokeStyle)>();

        private Matrix3x2 viewTransform = Matrix3x2.Identity;

        #endregion Fields

        #region Constructors

        public ArtView()
        {
            RenderMode = RenderMode.Manual;
            RenderTargetBound += OnRenderTargetBound;
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

        #endregion Properties

        #region Methods

        public void ClearSelection()
        {
            while (Selection.Count > 0)
                Selection[0].Selected = false;
        }

        protected override async void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            moved = false;

            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

            var root = Root;
            var hit = await Task.Run(() => root.Hit(factory, pos));

            if (hit == null)
                ClearSelection();

            if (Selection.Count() > 0)
            {
                List<(Vector2 pos, ArtViewHandle handle)> handles = new List<(Vector2, ArtViewHandle)>();

                float x1 = selectionBounds.Left, y1 = selectionBounds.Top,
                    x2 = selectionBounds.Right, y2 = selectionBounds.Bottom;

                handles.Add((new Vector2(x1, y1), ArtViewHandle.TopLeft));
                handles.Add((new Vector2(x2, y1), ArtViewHandle.TopRight));
                handles.Add((new Vector2(x2, y2), ArtViewHandle.BottomRight));
                handles.Add((new Vector2(x1, y2), ArtViewHandle.BottomLeft));
                handles.Add((new Vector2((x1 + x2) / 2, y1), ArtViewHandle.Top));
                handles.Add((new Vector2(x1, (y1 + y2) / 2), ArtViewHandle.Left));
                handles.Add((new Vector2(x2, (y1 + y2) / 2), ArtViewHandle.Right));
                handles.Add((new Vector2((x1 + x2) / 2, y2), ArtViewHandle.Bottom));

                foreach (var h in handles)
                {
                    if ((h.pos.X - pos.X) * (h.pos.X - pos.X) +
                        (h.pos.Y - pos.Y) * (h.pos.Y - pos.Y) < 6.25)
                    {
                        currentHandle = h.handle;
                        return;
                    }
                }

                foreach (var l in Selection)
                {
                    if (hit != null)
                        currentHandle = ArtViewHandle.Center;
                }
            }

            lastPosition = pos;

            UpdateSelectionBounds();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            moved = true;

            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

            lastPosition = lastPosition ?? pos;

            float width = Math.Max(1f, selectionBounds.Width),
                height = Math.Max(1f, selectionBounds.Height);

            if (currentHandle != null)
            {
                switch (currentHandle)
                {
                    case ArtViewHandle.TopLeft:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                -(pos.X - selectionBounds.Right) / width,
                                -(pos.Y - selectionBounds.Bottom) / height,
                                selectionBounds.BottomRight);
                        // if (selectionTransform.ScaleVector.Y < 0) Debugger.Break();
                        break;

                    case ArtViewHandle.Top:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                1,
                                -(pos.Y - selectionBounds.Bottom) / height,
                                new Vector2(selectionBounds.Center.X, selectionBounds.Bottom));
                        break;

                    case ArtViewHandle.TopRight:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                (pos.X - selectionBounds.Left) / width,
                                -(pos.Y - selectionBounds.Bottom) / height,
                                selectionBounds.BottomLeft);
                        break;

                    case ArtViewHandle.Left:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                -(pos.X - selectionBounds.Left) / width,
                                1,
                                new Vector2(selectionBounds.Right, selectionBounds.Center.Y));
                        break;

                    case ArtViewHandle.Center:
                        selectionTransform = Matrix3x2.Translation(pos - lastPosition.Value);
                        break;

                    case ArtViewHandle.Right:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                (pos.X - selectionBounds.Left) / width,
                                1,
                                new Vector2(selectionBounds.Left, selectionBounds.Center.Y));
                        break;

                    case ArtViewHandle.BottomLeft:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                -(pos.X - selectionBounds.Right) / width,
                                (pos.Y - selectionBounds.Top) / height,
                                selectionBounds.TopRight);
                        break;

                    case ArtViewHandle.Bottom:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                1,
                                (pos.Y - selectionBounds.Top) / height,
                                new Vector2(selectionBounds.Center.X, selectionBounds.Top));
                        break;

                    case ArtViewHandle.BottomRight:
                        selectionTransform =
                            Matrix3x2.Scaling(
                                (pos.X - selectionBounds.Left) / width,
                                (pos.Y - selectionBounds.Top) / height,
                                selectionBounds.TopLeft);
                        break;
                }

                if (selectionTransform.ScaleVector.X < 0)
                {
                    switch (currentHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            currentHandle = ArtViewHandle.TopRight;
                            break;
                        case ArtViewHandle.TopRight:
                            currentHandle = ArtViewHandle.TopLeft;
                            break;
                        case ArtViewHandle.Left:
                            currentHandle = ArtViewHandle.Right;
                            break;
                        case ArtViewHandle.Right:
                            currentHandle = ArtViewHandle.Left;
                            break;
                        case ArtViewHandle.BottomLeft:
                            currentHandle = ArtViewHandle.BottomRight;
                            break;
                        case ArtViewHandle.BottomRight:
                            currentHandle = ArtViewHandle.BottomLeft;
                            break;
                    }
                }

                if (selectionTransform.ScaleVector.Y < 0)
                {
                    switch (currentHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            currentHandle = ArtViewHandle.BottomLeft;
                            break;
                        case ArtViewHandle.TopRight:
                            currentHandle = ArtViewHandle.BottomRight;
                            break;
                        case ArtViewHandle.Top:
                            currentHandle = ArtViewHandle.Bottom;
                            break;
                        case ArtViewHandle.Bottom:
                            currentHandle = ArtViewHandle.Top;
                            break;
                        case ArtViewHandle.BottomLeft:
                            currentHandle = ArtViewHandle.TopLeft;
                            break;
                        case ArtViewHandle.BottomRight:
                            currentHandle = ArtViewHandle.TopRight;
                            break;
                    }
                }

                if (Selection.Count() > 0)
                    foreach (var layer in Selection)
                        layer.Transform(selectionTransform);

                Debug.Print($"bounds: {selectionBounds} transform: {selectionTransform} pos: {pos}");

                selectionTransform = Matrix.Identity;

                UpdateSelectionBounds();
            }

            if (Selection.Count() > 0)
            {
                List<(Vector2 pos, Cursor cur)> handles = new List<(Vector2, Cursor)>();

                Vector2 tl = selectionBounds.TopLeft,
                    br = selectionBounds.BottomRight;

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

            lastPosition = pos;

            base.OnMouseMove(e);
        }

        protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

            await OnMouseUp(pos);
        }

        private async Task OnMouseUp(Vector2 pos)
        {
            currentHandle = null;

            selectionTransform = Matrix3x2.Identity;

            if (!moved)
            {

                var root = Root;
                var hit = await Task.Run(() => root.Hit(factory, pos));

                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    ClearSelection();

                if (hit != null)
                    hit.Selected = true;
            }

            UpdateSelectionBounds();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            float scale = 1 + e.Delta / 500f;
            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                viewTransform *= Matrix3x2.Scaling(scale, scale, pos);

            InvalidateSurface(null);

            base.OnMouseWheel(e);
        }

        protected override async void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            await OnMouseUp(lastPosition ?? Vector2.Zero);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            factory = target.Factory;
            renderTarget = target;

            LoadBrushes(target);

            BindLayer(Root, target);
        }

        protected override void Render(RenderTarget target)
        {
            void RenderLayer(Model.Layer layer)
            {
                if (layer is Model.Shape shape)
                {
                    if (shape.FillBrush != null)
                        target.FillGeometry(shape.GetGeometry(target.Factory), fills[layer]);

                    if (shape.StrokeBrush != null)
                        target.DrawGeometry(
                            shape.GetGeometry(target.Factory),
                            strokes[layer].Item1,
                            strokes[layer].Item2,
                            strokes[layer].Item3);
                }

                foreach (var subLayer in layer.SubLayers.Reverse())
                    RenderLayer(subLayer);
            }

            target.Clear(Color4.White);

            target.Transform = viewTransform;

            RenderLayer(Root);

            if (Selection.Count() > 0)
            {
                // draw selection outline
                // var bounds = MathUtils.Transform2D(selectionBounds, selectionTransform);
                target.DrawRectangle(selectionBounds, brushes["A1"], 1f / viewTransform.ScaleVector.X);

                // draw handles
                List<Vector2> handles = new List<Vector2>();

                float x1 = selectionBounds.Left, y1 = selectionBounds.Top,
                    x2 = selectionBounds.Right, y2 = selectionBounds.Bottom;

                handles.Add(new Vector2(x1, y1));
                handles.Add(new Vector2(x2, y1));
                handles.Add(new Vector2(x2, y2));
                handles.Add(new Vector2(x1, y2));
                handles.Add(new Vector2((x1 + x2) / 2, y1));
                handles.Add(new Vector2(x1, (y1 + y2) / 2));
                handles.Add(new Vector2(x2, (y1 + y2) / 2));
                handles.Add(new Vector2((x1 + x2) / 2, y2));

                float handleSize = 5f / viewTransform.ScaleVector.X;

                foreach (Vector2 v in handles)
                {
                    Ellipse e = new Ellipse(v, handleSize, handleSize);
                    target.FillEllipse(e, brushes["A1"]);
                    target.DrawEllipse(e, brushes["L1"], 2f / Zoom);
                }
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

            av.UpdateSelectionBounds();
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            av.viewTransform.ScaleVector = new Vector2(av.Zoom);
        }

        private Brush BindBrush(Model.Shape shape, Model.BrushInfo brush, RenderTarget target)
        {
            if (brush == null) return null;

            Brush fill = null;
            BrushProperties props = new BrushProperties()
            {
                Opacity = brush.Opacity,
                Transform = brush.Transform
            };

            switch (brush.BrushType)
            {
                case Model.BrushType.Color:
                    fill = new SolidColorBrush(target, brush.Color, props);

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

                        InvalidateSurface((Rectangle)shape.GetBounds());
                    };
                    break;

                case Model.BrushType.LinearGradient:
                    fill = new LinearGradientBrush(
                        target,
                        new LinearGradientBrushProperties()
                        {
                            StartPoint = brush.StartPoint,
                            EndPoint = brush.EndPoint
                        },
                        props,
                        new GradientStopCollection(target, brush.Stops.ToArray()));
                    break;

                case Model.BrushType.RadialGradient:
                    fill = new RadialGradientBrush(
                        target,
                        new RadialGradientBrushProperties()
                        {
                            Center = brush.StartPoint,
                            RadiusX = brush.EndPoint.X - brush.StartPoint.X,
                            RadiusY = brush.EndPoint.Y - brush.StartPoint.Y
                        },
                        props,
                        new GradientStopCollection(target, brush.Stops.ToArray()));
                    break;
            }

            return fill;
        }

        private void BindLayer(Model.Layer layer, RenderTarget target)
        {
            if (layer is Model.Shape shape)
            {
                if (shape.FillBrush != null)
                    fills[shape] = BindBrush(shape, shape.FillBrush, target);
                if (shape.StrokeBrush != null)
                    strokes[shape] =
                        (BindBrush(shape, shape.StrokeBrush, target), shape.StrokeWidth, new StrokeStyle(target.Factory, shape.StrokeStyle));
            }

            foreach (var subLayer in layer.SubLayers)
                BindLayer(subLayer, target);
        }

        private RectangleF GetBounds(Model.Layer layer)
        {
            return bounds[layer] = bounds.ContainsKey(layer) ? bounds[layer] : layer.GetBounds();
        }

        private void LoadBrushes(RenderTarget target)
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

        private void OnRootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // this event fires whenever any of the layers changes anything, not just the root layer
            var layer = sender as Model.Layer;
            bounds[layer] = layer.GetBounds();

            if (layer is Model.Shape shape)
            {
                if (e.PropertyName == nameof(shape.FillBrush))
                {
                    fills[layer].Dispose();
                    fills[layer] = BindBrush(shape, shape.FillBrush, renderTarget);
                }

                if (e.PropertyName == nameof(shape.StrokeBrush))
                {
                    strokes[layer].brush.Dispose();
                    strokes[layer] = (BindBrush(shape, shape.StrokeBrush, renderTarget), strokes[layer].width, strokes[layer].style);
                }

                if (e.PropertyName == nameof(shape.StrokeWidth))
                {
                    strokes[layer] = (strokes[layer].brush, shape.StrokeWidth, strokes[layer].style);
                }

                if (e.PropertyName == nameof(shape.StrokeStyle))
                {
                    strokes[layer].style.Dispose();
                    strokes[layer] = (strokes[layer].brush, strokes[layer].width, new StrokeStyle(factory, shape.StrokeStyle));
                }
            }

            UpdateSelectionBounds();
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateSelectionBounds();
        }

        private void UpdateSelectionBounds()
        {
            var dirty = (Rectangle)selectionBounds;
            dirty.Inflate(10, 10);

            InvalidateSurface(dirty);

            if (Selection.Count == 0)
            {
                selectionBounds = RectangleF.Empty;
                return;
            }

            Model.Layer first = Selection.FirstOrDefault();
            (float x1, float y1, float x2, float y2) = (first?.X ?? 0, first?.Y ?? 0, first?.X ?? 1, first?.Y ?? 1);

            Parallel.ForEach(Selection, l =>
            {
                var b = l.GetBounds();

                if (b.Left < x1) x1 = b.Left;
                if (b.Top < y1) y1 = b.Top;
                if (b.Right > x2) x2 = b.Right;
                if (b.Bottom > y2) y2 = b.Bottom;
            });

            selectionBounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

            // can't be smaller than 1x1 or else we get division by zero
            // in other places
            //selectionBounds.Width = Math.Max(1, selectionBounds.Width);
            //selectionBounds.Height = Math.Max(1, selectionBounds.Height);

            dirty = (Rectangle)selectionBounds;
            dirty.Inflate(10, 10);
            InvalidateSurface(dirty);
        }

        protected override void InvalidateSurface(Rectangle? area)
        {
            //var rect = MathUtils.Transform2D(
            //    new RectangleF(
            //        area?.X ?? 0, 
            //        area?.Y ?? 0, 
            //        area?.Width ?? 0, 
            //        area?.Height ?? 0), 
            //    viewTransform);
            base.InvalidateSurface(null);
        }

        #endregion Methods
    }
}