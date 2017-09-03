using System.Collections.Generic;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Imaging;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Color = System.Windows.Media.Color;
using Factory1 = SharpDX.Direct2D1.Factory1;
using Image = System.Drawing.Image;
using Layer = Ibinimator.Model.Layer;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace Ibinimator.Service
{
    public class CacheManager : Model.Model, ICacheManager
    {
        private readonly Dictionary<string, Bitmap> _bitmaps = new Dictionary<string, Bitmap>();
        private readonly Dictionary<Layer, RectangleF> _bounds = new Dictionary<Layer, RectangleF>();

        private readonly Dictionary<BrushInfo, (Shape shape, Brush brush)> _brushBindings =
            new Dictionary<BrushInfo, (Shape, Brush)>();

        private readonly Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();

        private readonly Dictionary<Shape, Brush> _fills = new Dictionary<Shape, Brush>();
        private readonly Dictionary<Shape, Geometry> _geometries = new Dictionary<Shape, Geometry>();
        private readonly Dictionary<Layer, BitmapRenderTarget> _renders = new Dictionary<Layer, BitmapRenderTarget>();

        private readonly Dictionary<Shape, (Brush brush, float width, StrokeStyle style)> _strokes =
            new Dictionary<Shape, (Brush, float, StrokeStyle)>();

        public CacheManager(ArtView artView)
        {
            ArtView = artView;
        }

        #region ICacheManager Members

        public ArtView ArtView { get; set; }

        public void Bind(Document doc)
        {
            doc.Updated -= OnLayerPropertyChanged;
            doc.Updated += OnLayerPropertyChanged;
            BindLayer(doc.Root);
        }

        public Brush BindBrush(Shape shape, BrushInfo brush)
        {
            if (brush == null) return null;

            var target = ArtView.RenderTarget;

            var fill = brush.ToDirectX(target);
            brush.PropertyChanged += OnBrushPropertyChanged;

            lock (_brushBindings)
            {
                _brushBindings[brush] = (shape, fill);
            }

            return fill;
        }

        public void BindLayer(Layer layer)
        {
            var target = ArtView.RenderTarget;

            if (layer is Shape shape)
            {
                if (shape.FillBrush != null)
                    lock (_fills)
                    {
                        _fills[shape] = BindBrush(shape, shape.FillBrush);
                    }
                if (shape.StrokeBrush != null)
                    lock (_strokes)
                    {
                        if (shape.StrokeStyle.DashStyle == DashStyle.Custom)
                            _strokes[shape] =
                                (
                                BindBrush(shape, shape.StrokeBrush),
                                shape.StrokeWidth,
                                new StrokeStyle1(
                                    target.Factory.QueryInterface<Factory1>(),
                                    shape.StrokeStyle,
                                    shape.StrokeDashes.ToArray()
                                ));
                        else
                            _strokes[shape] =
                                (
                                BindBrush(shape, shape.StrokeBrush),
                                shape.StrokeWidth,
                                new StrokeStyle1(
                                    target.Factory.QueryInterface<Factory1>(),
                                    shape.StrokeStyle
                                ));
                    }
            }

            if (layer is Group group)
            {
                foreach (var subLayer in group.SubLayers)
                    BindLayer(subLayer);

                group.LayerAdded +=
                    (sender, layer1) =>
                        BindLayer(layer1);
            }
        }

        public RectangleF GetAbsoluteBounds(Layer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.AbsoluteTransform);
        }

        public Bitmap GetBitmap(string key)
        {
            return _bitmaps[key];
        }

        public RectangleF GetBounds(Layer layer)
        {
            return Get(_bounds, layer, l =>
            {
                if (l is Group g)
                    return g.SubLayers
                        .Select(GetRelativeBounds)
                        .Aggregate(RectangleF.Union);

                return l.GetBounds();
            });
        }

        public Brush GetBrush(string key)
        {
            return _brushes[key];
        }

        public Brush GetFill(Shape layer)
        {
            return Get(_fills, layer, l => l.FillBrush.ToDirectX(ArtView.RenderTarget));
        }

        public Geometry GetGeometry(Shape layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(ArtView.RenderTarget.Factory));
        }

        public RectangleF GetRelativeBounds(Layer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public (Brush brush, float width, StrokeStyle style) GetStroke(Shape layer, RenderTarget target)
        {
            return Get(_strokes, layer, l =>
            {
                if (l.StrokeStyle.DashStyle == DashStyle.Solid)
                    return (
                        l.StrokeBrush.ToDirectX(target),
                        l.StrokeWidth,
                        new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), l.StrokeStyle));

                return (
                    l.StrokeBrush.ToDirectX(target),
                    l.StrokeWidth,
                    new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), l.StrokeStyle,
                        l.StrokeDashes.ToArray()));
            });
        }

        public void LoadBitmaps(RenderTarget target)
        {
            _bitmaps["cursor-ns"] = LoadBitmap(target, "resize-ns");
            _bitmaps["cursor-ew"] = LoadBitmap(target, "resize-ew");
            _bitmaps["cursor-nwse"] = LoadBitmap(target, "resize-nwse");
            _bitmaps["cursor-nesw"] = LoadBitmap(target, "resize-nesw");
            _bitmaps["cursor-rot"] = LoadBitmap(target, "rotate");
        }

        public void LoadBrushes(RenderTarget target)
        {
            foreach (DictionaryEntry entry in Application.Current.Resources)
                if (entry.Value is Color color)
                    _brushes[(string) entry.Key] =
                        new SolidColorBrush(
                            target,
                            new RawColor4(
                                color.R / 255f,
                                color.G / 255f,
                                color.B / 255f,
                                color.A / 255f));
        }

        public void ResetAll()
        {
            ResetLayerCache();

            lock (_brushes)
            {
                foreach (var (_, brush) in _brushes.AsTuples())
                    brush?.Dispose();
                _brushes.Clear();
            }

            lock (_bitmaps)
            {
                foreach (var (_, bitmap) in _bitmaps.AsTuples()) bitmap?.Dispose();
                _bitmaps.Clear();
            }
        }

        public void ResetLayerCache()
        {
            lock (_renders)
            {
                foreach (var (_, render) in _renders.AsTuples())
                    render.Dispose();

                _renders.Clear();
            }

            lock (_brushBindings)
            {
                foreach (var (brushInfo, (_, brush)) in _brushBindings.AsTuples())
                {
                    brushInfo.PropertyChanged -= OnBrushPropertyChanged;
                    brush?.Dispose();
                }
                _brushBindings.Clear();
            }

            lock (_geometries)
            {
                foreach (var (_, geometry) in _geometries.AsTuples()) geometry?.Dispose();
                _geometries.Clear();
            }

            lock (_fills)
            {
                foreach (var (_, fill) in _fills.AsTuples()) fill?.Dispose();
                _fills.Clear();
            }

            lock (_strokes)
            {
                foreach (var (_, (stroke, _, style)) in _strokes.AsTuples())
                {
                    stroke?.Dispose();
                    style?.Dispose();
                }
                _strokes.Clear();
            }
        }

        #endregion

        public void UnbindLayer(Layer layer)
        {
            if (layer is Shape shape)
            {
                if (shape.FillBrush != null)
                    lock (_fills)
                    {
                        _fills[shape]?.Dispose();
                        _fills.Remove(shape);
                    }

                if (shape.StrokeBrush != null)
                    lock (_strokes)
                    {
                        _strokes[shape].brush.Dispose();
                        _strokes[shape].style.Dispose();
                        _strokes.Remove(shape);
                    }

                lock (_geometries)
                {
                    _geometries[shape].Dispose();
                    _geometries.Remove(shape);
                }
            }

            lock (_bounds)
            {
                _bounds.Remove(layer);
            }

            if (layer is Group group)
                foreach (var subLayer in group.SubLayers)
                    UnbindLayer(subLayer);
        }

        private static TV Get<TK, TV>(Dictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
        {
            lock (dict)
            {
                if (dict.TryGetValue(key, out TV value) &&
                    (value as DisposeBase)?.IsDisposed != true)
                    return value;

                return dict[key] = fallback(key);
            }
        }

        private void InvalidateLayer(Layer layer)
        {
            ArtView.InvalidateSurface();
        }

        private Bitmap LoadBitmap(RenderTarget target, string name)
        {
            using (var stream = Application
                .GetResourceStream(new Uri($"./Resources/Icon/{name}.png", UriKind.Relative))
                .Stream)
            {
                using (var bitmap = (System.Drawing.Bitmap) Image.FromStream(stream))
                {
                    var sourceArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var bitmapProperties = new BitmapProperties(
                        new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                        target.DotsPerInch.Width, target.DotsPerInch.Height);

                    var data = bitmap.LockBits(sourceArea,
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                    var dpi = target.DotsPerInch.Width / 96;

                    using (var temp = new DataStream(data.Scan0, bitmap.Width * sizeof(int), true, true))
                    {
                        var bmp = new Bitmap(target, new Size2(sourceArea.Width, sourceArea.Height), temp, data.Stride,
                            bitmapProperties);

                        bitmap.UnlockBits(data);

                        return bmp;
                    }
                }
            }
        }

        private void OnBrushPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var brush = (BrushInfo) sender;
            var (shape, fill) = Get(_brushBindings, brush, k => (null, null));

            switch (e.PropertyName)
            {
                case "Opacity":
                    fill.Opacity = brush.Opacity;
                    break;

                case "Transform":
                    fill.Transform = brush.Transform;
                    break;

                case "Color":
                    ((SolidColorBrush) fill).Color = ((SolidColorBrushInfo) brush).Color;
                    break;
            }

            InvalidateLayer(shape);
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = (Layer) sender;
            var shape = sender as Shape;

            switch (e.PropertyName)
            {
                case "Geometry":
                    lock (_geometries)
                    {
                        _geometries.TryGet(shape)?.Dispose();
                        _geometries[shape] = shape.GetGeometry(ArtView.RenderTarget.Factory);
                    }
                    goto case "Bounds";

                case nameof(Shape.FillBrush):
                    lock (_fills)
                    {
                        _fills.TryGet(shape)?.Dispose();
                        _fills[shape] = BindBrush(shape, shape.FillBrush);
                    }
                    break;

                case nameof(Shape.StrokeBrush):
                    lock (_strokes)
                    {
                        var stroke = _strokes.TryGet(shape);
                        stroke.brush?.Dispose();
                        stroke.brush = BindBrush(shape, shape.StrokeBrush);
                        _strokes[shape] = stroke;
                    }
                    break;

                case nameof(Shape.StrokeWidth):
                    lock (_strokes)
                    {
                        var stroke = _strokes.TryGet(shape);
                        stroke.width = shape.StrokeWidth;
                        _strokes[shape] = stroke;
                    }
                    break;

                case nameof(Shape.StrokeStyle):
                case nameof(Shape.StrokeDashes):
                    lock (_strokes)
                    {
                        var stroke = _strokes.TryGet(shape);
                        stroke.style?.Dispose();

                        if (shape.StrokeStyle.DashStyle == DashStyle.Custom)
                            stroke.style =
                                new StrokeStyle1(
                                    ArtView.RenderTarget.Factory.QueryInterface<Factory1>(),
                                    shape.StrokeStyle,
                                    shape.StrokeDashes.ToArray());
                        else
                            stroke.style =
                                new StrokeStyle1(
                                    ArtView.RenderTarget.Factory.QueryInterface<Factory1>(),
                                    shape.StrokeStyle);

                        _strokes[shape] = stroke;
                    }
                    break;

                case "Bounds":
                    lock (_bounds)
                    {
                        if (layer is Group g)
                            _bounds[layer] =
                                g.SubLayers.Select(GetRelativeBounds)
                                    .Aggregate(RectangleF.Union);
                        else
                            _bounds[layer] = layer.GetBounds();
                    }

                    if (layer.Parent != null)
                        OnLayerPropertyChanged(layer.Parent, new PropertyChangedEventArgs("Bounds"));
                    break;

                case nameof(Layer.Transform):
                    if (layer.Parent != null)
                        OnLayerPropertyChanged(layer.Parent, new PropertyChangedEventArgs("Bounds"));
                    break;
            }

            InvalidateLayer(layer);
        }
    }
}