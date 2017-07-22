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
using System.Diagnostics;

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

        internal Matrix3x2 ViewTransform => viewTransform;

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
            cacheHelper.LoadBitmaps(target);

            cacheHelper.BindLayer(Root);
        }

        protected override void Render(RenderTarget target)
        {
            target.Clear(Color4.White);

            target.Transform = viewTransform;

            Root.Render(target, cacheHelper);

            selectionHelper.Render(target, cacheHelper.GetBrush("A1"), cacheHelper.GetBrush("L1"));
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
            av.selectionHelper.UpdateSelection(true);
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
            selectionHelper.UpdateSelection(false);
        }

        private void OnSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            selectionHelper.UpdateSelection(true);
        }

        #endregion Methods
    }

    public class CacheHelper : Model.Model
    {
        #region Fields

        private Dictionary<string, Bitmap> bitmaps = new Dictionary<string, Bitmap>();
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

        public Bitmap GetBitmap(string key) => bitmaps[key];

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

        public void LoadBitmaps(RenderTarget target)
        {
            bitmaps["cursor-ns"] = LoadBitmap(target, "resize-ns");
            bitmaps["cursor-ew"] = LoadBitmap(target, "resize-ew");
            bitmaps["cursor-nwse"] = LoadBitmap(target, "resize-nwse");
            bitmaps["cursor-nesw"] = LoadBitmap(target, "resize-nesw");
            bitmaps["cursor-rot"] = LoadBitmap(target, "rotate");
        }

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

                    if (bubble != true)
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

        private unsafe Bitmap LoadBitmap(RenderTarget target, string name)
        {
            using (var stream = App.GetResourceStream(new Uri($"./Resources/Icon/{name}.png", UriKind.Relative)).Stream)
            {
                var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream);
                var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapProperties = new BitmapProperties(
                    new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), 192, 192);

                var data = bitmap.LockBits(sourceArea,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                using (var temp = new DataStream(data.Scan0, bitmap.Width * sizeof(int), true, true))
                {
                    var bmp = new Bitmap(target, new Size2(sourceArea.Width, sourceArea.Height), temp, data.Stride, bitmapProperties);

                    bitmap.UnlockBits(data);

                    return bmp;
                }
            }
        }

        #endregion Methods
    }

    internal class SelectionHelper : Model.Model
    {
        #region Fields

        public RectangleF SelectionBounds;
        public RectangleF SelectionBox;
        public float SelectionRotation;
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
        public Bitmap Cursor { get; set; }
        public ArtViewHandle? ResizingHandle { get; set; }
        public Model.Layer Root => ArtView?.Dispatcher.Invoke(() => ArtView.Root);
        public IList<Model.Layer> Selection { get; set; }

        #endregion Properties

        #region Methods

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
                    var test = HandleTest(pos);
                    ResizingHandle = test.handle;
                    bool hit = test.handle != null;

                    if (!hit)
                    {
                        foreach (var l in Selection)
                        {
                            if (l.Hit(factory, pos, l.Parent.AbsoluteTransform) != null)
                            {
                                ResizingHandle = ArtViewHandle.Translation;
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

                UpdateSelection(false);
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
                    Vector2 scale = Vector2.One;
                    Vector2 scaleOrigin = Vector2.Zero;
                    Vector2 translate = Vector2.Zero;
                    Matrix3x2 rotateSkew = Matrix.Identity;
                    Vector2 rpos = Matrix3x2.TransformPoint(Matrix3x2.Rotation(-SelectionRotation, SelectionBounds.Center), pos);

                    switch (ResizingHandle)
                    {
                        //case ArtViewHandle.TopLeft:
                        //    scale = 
                        //        new Vector2(
                        //            -(rpos.X - SelectionBounds.Right) / width,
                        //            -(rpos.Y - SelectionBounds.Bottom) / height);
                        //    scaleOrigin = Vector2.Zero;
                        //    break;

                        case ArtViewHandle.Top:
                            scale = new Vector2(1, -(rpos.Y - SelectionBounds.Bottom) / height);
                            scaleOrigin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Bottom);
                            break;

                        case ArtViewHandle.Bottom:
                            scale = new Vector2(1, (rpos.Y - SelectionBounds.Top) / height);
                            scaleOrigin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Top);
                            break;

                        //case ArtViewHandle.TopRight:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            (rpos.X - SelectionBounds.Left) / width,
                        //            -(rpos.Y - SelectionBounds.Bottom) / height,
                        //            Matrix3x2.TransformPoint(transform, SelectionBounds.BottomLeft));
                        //    break;

                        //case ArtViewHandle.Left:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            -(rpos.X - SelectionBounds.Left) / width,
                        //            1,
                        //            Matrix3x2.TransformPoint(transform,
                        //                new Vector2(SelectionBounds.Right, SelectionBounds.Center.Y)));
                        //    break;

                        case ArtViewHandle.Translation:
                            translate = pos - lastPosition.Value;
                            break;

                        //case ArtViewHandle.Right:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            (rpos.X - SelectionBounds.Left) / width,
                        //            1,
                        //            Matrix3x2.TransformPoint(transform,
                        //                new Vector2(SelectionBounds.Left, SelectionBounds.Center.Y)));
                        //    break;

                        //case ArtViewHandle.BottomLeft:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            -(rpos.X - SelectionBounds.Right) / width,
                        //            (rpos.Y - SelectionBounds.Top) / height,
                        //            Matrix3x2.TransformPoint(transform, SelectionBounds.TopRight));
                        //    break;

                        //case ArtViewHandle.Bottom:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            1,
                        //            (rpos.Y - SelectionBounds.Top) / height,
                        //            Matrix3x2.TransformPoint(transform,
                        //                new Vector2(SelectionBounds.Center.X, SelectionBounds.Top)));
                        //    break;

                        //case ArtViewHandle.BottomRight:
                        //    scaleTranslate =
                        //        Matrix3x2.Scaling(
                        //            (rpos.X - SelectionBounds.Left) / width,
                        //            (rpos.Y - SelectionBounds.Top) / height,
                        //            Matrix3x2.TransformPoint(transform, SelectionBounds.TopLeft));
                        //    break;

                        case ArtViewHandle.Rotation:
                            var x = pos - SelectionBounds.Center;
                            var r = -(float)(Math.Atan2(-x.Y, x.X) - MathUtil.PiOverTwo);
                            rotateSkew = Matrix3x2.Rotation(r - SelectionRotation, SelectionBounds.Center - SelectionBounds.TopLeft);
                            SelectionRotation = r;

                            if (r - SelectionRotation > 0.1f)
                                Debugger.Break();
                            break;
                    }
                    
                    scale.X = MathUtils.AbsMax(0.001f, scale.X);
                    scale.Y = MathUtils.AbsMax(0.001f, scale.Y);

                    scale = new Vector2(1, 1.1f);

                    if (scale.X < 0)
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

                    if (scale.Y < 0)
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

                    Matrix3x2 scaleMat = Matrix3x2.Scaling(scale.X, scale.Y);
                    Matrix3x2 translateMat = Matrix3x2.Translation(translate);

                    foreach (var layer in Selection)
                    {
                        var origin = scaleOrigin - (layer.WorldTransform * layer.Translate).TranslationVector;
                        var offset = 
                            Matrix3x2.TransformPoint(
                                layer.RotationSkew * Matrix3x2.Translation(-layer.RotationSkew.TranslationVector), 
                                (Vector2.One - scale) * origin);
                        
                        layer.Scale *= scaleMat;
                        layer.RotationSkew *= rotateSkew;
                        layer.Translate *= translateMat * Matrix3x2.Translation(offset);
                    }

                    UpdateSelection(false);
                }

                UpdateCursor(pos);

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

                UpdateSelection(false);
            }
        }

        public void Render(RenderTarget target, Brush fill, Brush stroke)
        {
            void RenderHandles(RectangleF rect, Matrix3x2 transform, Brush accent)
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
                handles.Add(new Vector2((x1 + x2) / 2, y1 - 10));

                var scale = ((Matrix3x2)target.Transform).ScaleVector;

                foreach (Vector2 v in handles.Select(v => Matrix3x2.TransformPoint(transform, v)))
                {
                    Ellipse e = new Ellipse(v, 5f / scale.X, 5f / scale.Y);
                    target.FillEllipse(e, accent);
                    target.DrawEllipse(e, stroke, 2f / scale.X);
                }
            }

            if (Selection.Count == 1)
            {
                var layer = Selection[0];

                if (layer is Model.Shape shape)
                {
                    target.Transform *= shape.Parent.AbsoluteTransform;
                    target.DrawGeometry(ArtView.cacheHelper.GetGeometry(shape), fill, 1f / target.Transform.M11);
                    target.Transform *= Matrix3x2.Invert(shape.Parent.AbsoluteTransform);
                }

                RectangleF rect = layer.GetBounds();

                using (RectangleGeometry rg = new RectangleGeometry(target.Factory, rect))
                using (TransformedGeometry tg = new TransformedGeometry(target.Factory, rg, layer.AbsoluteTransform))
                    target.DrawGeometry(tg, fill, 1f / target.Transform.M11);

                RenderHandles(rect, layer.AbsoluteTransform, fill);

                RenderHandles(SelectionBounds, Matrix3x2.Identity, ArtView.cacheHelper.GetBrush("A2"));

                RenderHandles(SelectionBounds, Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center),
                    ArtView.cacheHelper.GetBrush("A3"));

                RenderHandles(MathUtils.Bounds(SelectionBounds, Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center)), Matrix3x2.Identity,
                    ArtView.cacheHelper.GetBrush("A4"));
            }
            if (Selection.Count > 1)
            {
                foreach (var layer in Selection)
                    if (layer is Model.Shape shape)
                    {
                        target.Transform *= shape.Parent.AbsoluteTransform;
                        target.DrawGeometry(ArtView.cacheHelper.GetGeometry(shape), fill, 1f / target.Transform.M11);
                        target.Transform *= Matrix3x2.Invert(shape.Parent.AbsoluteTransform);
                    }

                target.Transform *= Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center);

                // draw selection outline
                target.DrawRectangle(SelectionBounds, fill, 1f / target.Transform.M11);

                target.Transform *= Matrix3x2.Rotation(-SelectionRotation, SelectionBounds.Center);

                RenderHandles(SelectionBounds, Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center), fill);
            }

            if (!SelectionBox.IsEmpty)
            {
                target.DrawRectangle(SelectionBox, fill, 1f / target.Transform.M11);
                fill.Opacity = 0.25f;
                target.FillRectangle(SelectionBox, fill);
                fill.Opacity = 1.0f;
            }

            if (Cursor != null)
            {
                target.Transform = Matrix3x2.Scaling(1f / 2) * Matrix3x2.Translation(lastPosition.Value - new Vector2(8));
                target.DrawBitmap(Cursor, 1, BitmapInterpolationMode.NearestNeighbor);
            }
        }

        public void UpdateSelection(bool reset)
        {
            InvalidateSurface();

            switch (Selection.Count)
            {
                case 0:
                    SelectionBounds = RectangleF.Empty;
                    return;

                case 1:
                    // to get accurate boundaries + rotation, must apply transform in same order (S * R * T)
                    SelectionBounds =
                        MathUtils.Bounds(
                            Selection[0].GetBounds(),
                            Selection[0].WorldTransform * Selection[0].Scale);

                    var rotatedBounds =
                        MathUtils.Bounds(
                            SelectionBounds,
                            Selection[0].RotationSkew);

                    SelectionBounds.Offset(rotatedBounds.Center - SelectionBounds.Center + Selection[0].Position);

                    if (reset)
                        SelectionRotation = Selection[0].RotationSkew.Decompose().rotation;
                    break;

                default:
                    RectangleF bounds = MathUtils.Bounds(ArtView.cacheHelper.GetBounds(Selection[0]), Selection[0].Parent.AbsoluteTransform);
                    (float x1, float y1, float x2, float y2) = (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

                    Parallel.ForEach(Selection.Skip(1), l =>
                    {
                        var b = MathUtils.Bounds(ArtView.cacheHelper.GetBounds(l), l.Parent.AbsoluteTransform);

                        if (b.Left < x1) x1 = b.Left;
                        if (b.Top < y1) y1 = b.Top;
                        if (b.Right > x2) x2 = b.Right;
                        if (b.Bottom > y2) y2 = b.Bottom;
                    });

                    SelectionBounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

                    if (reset)
                        SelectionRotation = 0;
                    break;
            }

            InvalidateSurface();
        }

        private (Bitmap cursor, ArtViewHandle? handle) HandleTest(Vector2 pos)
        {
            List<(Vector2 pos, string cur, ArtViewHandle handle)> handles = new List<(Vector2, string, ArtViewHandle)>();

            pos = Matrix3x2.TransformPoint(Matrix3x2.Rotation(-SelectionRotation, SelectionBounds.Center), pos);

            Vector2 tl = SelectionBounds.TopLeft,
                br = SelectionBounds.BottomRight;

            float x1 = tl.X, y1 = tl.Y,
                x2 = br.X, y2 = br.Y;

            handles.Add((new Vector2(x1, y1), "nwse", ArtViewHandle.TopLeft));
            handles.Add((new Vector2(x2, y1), "nesw", ArtViewHandle.TopRight));
            handles.Add((new Vector2(x2, y2), "nwse", ArtViewHandle.BottomRight));
            handles.Add((new Vector2(x1, y2), "nesw", ArtViewHandle.BottomLeft));
            handles.Add((new Vector2((x1 + x2) / 2, y1), "ns", ArtViewHandle.Top));
            handles.Add((new Vector2(x1, (y1 + y2) / 2), "ew", ArtViewHandle.Left));
            handles.Add((new Vector2(x2, (y1 + y2) / 2), "ew", ArtViewHandle.Right));
            handles.Add((new Vector2((x1 + x2) / 2, y2), "ns", ArtViewHandle.Bottom));
            handles.Add((new Vector2((x1 + x2) / 2, y1 - 10), "rot", ArtViewHandle.Rotation));

            foreach (var h in handles)
            {
                if ((pos - h.pos).LengthSquared() < 25f / ArtView.ViewTransform.ScaleVector.LengthSquared())
                    return (ArtView.cacheHelper.GetBitmap("cursor-" + h.cur), h.handle);
            }

            return (null, null);
        }

        private void InvalidateSurface()
        {
            ArtView.InvalidateSurface(
                MathUtils.Bounds(
                    SelectionBounds,
                    Matrix3x2.Rotation(
                        SelectionRotation,
                        SelectionBounds.Center))
                        .Inflate(20));
        }

        private void UI(Action a) => ArtView.Dispatcher.Invoke(a);

        private T UI<T>(Func<T> a) => ArtView.Dispatcher.Invoke(a);

        private void UpdateCursor(Vector2 pos)
        {
            if (ResizingHandle == null)
                if (Selection.Count > 0 && SelectionBounds.Inflate(17).Contains(pos))
                    Cursor = HandleTest(pos).cursor;
                else Cursor = null;

            ArtView.InvalidateSurface(new RectangleF(lastPosition.Value.X - 12, lastPosition.Value.Y - 12, 24, 24));

            if (Cursor == null)
                UI(() => ArtView.Cursor = Cursors.Arrow);
            else
            {
                var vpos = new Vector2(
                    pos.X,
                    pos.Y);

                ArtView.InvalidateSurface(new RectangleF(vpos.X - 12, vpos.Y - 12, 24, 24));

                UI(() => ArtView.Cursor = Cursors.None);
            }
        }

        #endregion Methods
    }
}