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

namespace Ibinimator.View.Control
{
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

        private RectangleF selectionBounds = new RectangleF();

        private Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();

        private ArtViewHandle? currentHandle;

        private Dictionary<Model.Layer, Brush> fills = new Dictionary<Model.Layer, Brush>();

        private Dictionary<Model.Layer, Geometry> geometries = new Dictionary<Model.Layer, Geometry>();

        private Dictionary<Model.Layer, (Brush, float, StrokeStyle)> strokes =
            new Dictionary<Model.Layer, (Brush, float, StrokeStyle)>();

        private Matrix3x2 viewTransform = Matrix3x2.Identity;
        private Matrix3x2 selectionTransform = Matrix3x2.Identity;

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
        public IEnumerable<Model.Layer> Selection
        {
            get { return (IEnumerable<Model.Layer>)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public float Zoom
        {
            get { return (float)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        #endregion Properties

        #region Methods

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

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
                    if((h.pos.X - pos.X) * (h.pos.X - pos.X) + 
                        (h.pos.Y - pos.Y) * (h.pos.Y - pos.Y) < 6.25)
                        currentHandle = h.handle;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var pos1 = e.GetPosition(this);
            var pos = new Vector2((float)pos1.X, (float)pos1.Y) / viewTransform.ScaleVector - viewTransform.TranslationVector;

            if (Selection.Count() > 0)
            {
                List<(Vector2 pos, Cursor cur)> handles = new List<(Vector2, Cursor)>();

                Vector2 tl = MathUtils.Transform2D(selectionBounds.TopLeft, selectionTransform), 
                    br = MathUtils.Transform2D(selectionBounds.BottomRight, selectionTransform);

                float x1 = tl.X, y1 = tl.Y,
                    x2 = br.X, y2 = br.X;

                handles.Add((new Vector2(x1, y1), Cursors.SizeNWSE));
                handles.Add((new Vector2(x2, y1), Cursors.SizeNESW));
                handles.Add((new Vector2(x2, y2), Cursors.SizeNWSE));
                handles.Add((new Vector2(x1, y2), Cursors.SizeNESW));
                handles.Add((new Vector2((x1 + x2) / 2, y1), Cursors.SizeNS));
                handles.Add((new Vector2(x1, (y1 + y2) / 2), Cursors.SizeWE));
                handles.Add((new Vector2(x2, (y1 + y2) / 2), Cursors.SizeWE));
                handles.Add((new Vector2((x1 + x2) / 2, y2), Cursors.SizeNS));

                var handle = handles.FirstOrDefault(
                    h => (h.pos.X - pos.X) * (h.pos.X - pos.X) +
                            (h.pos.Y - pos.Y) * (h.pos.Y - pos.Y) < 6.25);

                if (handle.pos != null)
                    Cursor = handle.cur;
                else
                    Cursor = Cursors.Arrow;
            }
            
            switch (currentHandle)
            {
                case ArtViewHandle.TopLeft:
                    selectionTransform =
                        Matrix3x2.Scaling(
                            1 + (pos.X - selectionBounds.Right) / selectionBounds.Width,
                            1 + (pos.Y - selectionBounds.Bottom) / selectionBounds.Height,
                            selectionBounds.BottomRight);
                    break;
                case ArtViewHandle.Top:
                    selectionTransform =
                        Matrix3x2.Scaling(
                            1,
                            (pos.Y - selectionBounds.Bottom) / selectionBounds.Height,
                            new Vector2(selectionBounds.Center.X, selectionBounds.Bottom));
                    break;
                case ArtViewHandle.TopRight:
                    selectionTransform =
                        Matrix3x2.Scaling(
                            1 + (pos.X - selectionBounds.Left) / selectionBounds.Width,
                            1 + (pos.Y - selectionBounds.Bottom) / selectionBounds.Height,
                            selectionBounds.BottomLeft);
                    break;
                case ArtViewHandle.Left:
                    break;
                case ArtViewHandle.Center:
                    break;
                case ArtViewHandle.Right:
                    break;
                case ArtViewHandle.BottomLeft:
                    break;
                case ArtViewHandle.Bottom:
                    break;
                case ArtViewHandle.BottomRight:
                    break;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            currentHandle = null;

            selectionTransform = Matrix3x2.Identity;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            Zoom *= (1 + e.Delta / 500f);

            base.OnMouseWheel(e);
        }

        protected void OnRenderTargetBound(object sender, RenderTarget target)
        {
            Brush BindBrush(Model.BrushInfo brush)
            {
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

            void BindLayer(Model.Layer layer)
            {
                if (layer is Model.Shape shape)
                {
                    if (shape.FillBrush != null)
                        fills[shape] = BindBrush(shape.FillBrush);
                    if (shape.StrokeBrush != null)
                        strokes[shape] =
                            (BindBrush(shape.StrokeBrush), shape.StrokeWidth, new StrokeStyle(target.Factory, shape.StrokeStyle));
                }

                foreach (var subLayer in layer.SubLayers)
                    BindLayer(subLayer);
            }

            LoadBrushes(target);

            BindLayer(Root);
        }

        protected override void Render(RenderTarget target)
        {
            void RenderLayer(Model.Layer layer)
            {
                if (layer is Model.Shape shape)
                {
                    if (shape.FillBrush != null)
                    {
                        switch (shape)
                        {
                            case Model.Ellipse ellipse:
                                target.FillEllipse(
                                    new Ellipse(
                                        new Vector2(ellipse.CenterX, ellipse.CenterY),
                                        ellipse.RadiusX,
                                        ellipse.RadiusY),
                                    fills[layer]);
                                break;

                            case Model.Rectangle rect:
                                target.FillRectangle(
                                    new RawRectangleF(rect.X1, rect.Y1, rect.X2, rect.Y2),
                                    fills[layer]);
                                break;
                        }
                    }

                    if (shape.StrokeBrush != null)
                    {
                        switch (shape)
                        {
                            case Model.Ellipse ellipse:
                                target.DrawEllipse(
                                    new Ellipse(
                                        new Vector2(ellipse.CenterX, ellipse.CenterY),
                                        ellipse.RadiusX,
                                        ellipse.RadiusY),
                                    strokes[layer].Item1,
                                    strokes[layer].Item2,
                                    strokes[layer].Item3);
                                break;

                            case Model.Rectangle rect:
                                target.DrawRectangle(
                                    new RawRectangleF(rect.X1, rect.Y1, rect.X2, rect.Y2),
                                    strokes[layer].Item1,
                                    strokes[layer].Item2,
                                    strokes[layer].Item3);
                                break;
                        }
                    }
                }

                foreach (var subLayer in layer.SubLayers)
                {
                    if (subLayer.Selected)
                        target.Transform = viewTransform * selectionTransform;
                    else
                        target.Transform = viewTransform;

                    RenderLayer(subLayer);
                }
            }

            target.Clear(null);

            target.Transform = viewTransform;

            RenderLayer(Root);

            target.Transform = viewTransform * selectionTransform;

            if (Selection.Count() > 0)
            {
                // draw selection outline
                // we want the selection box to be the same size no matter what
                target.DrawRectangle(selectionBounds, brushes["A1"], 1f / Zoom);

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

                float handleSize = 5f / Zoom;

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
            av.Root.PropertyChanged += av.OnRootChanged;
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            if (e.OldValue is INotifyCollectionChanged old)
                old.CollectionChanged -= av.OnSelectionUpdated;
            if (e.NewValue is INotifyCollectionChanged incc)
                incc.CollectionChanged += av.OnSelectionUpdated;

            av.Invalidate();
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArtView av = d as ArtView;
            av.viewTransform.ScaleVector = new Vector2(av.Zoom);

            av.Invalidate();
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

        private void OnRootChanged(object sender, PropertyChangedEventArgs e)
        {
            // this event fires whenever any of the layers changes anything, not just the root layer
            var layer = sender as Model.Layer;
            bounds[layer] = layer.GetBounds();
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            (float x1, float y1, float x2, float y2) = (0, 0, 0, 0);

            Parallel.ForEach(Selection, l =>
            {
                var b = l.GetBounds();

                if (b.Left < x1) x1 = b.Left;
                if (b.Top < y1) y1 = b.Top;
                if (b.Right > x2) x2 = b.Right;
                if (b.Bottom > y2) y2 = b.Bottom;
            });

            selectionBounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

            Invalidate();
        }

        #endregion Methods
    }

    internal enum ArtViewHandle
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }
}