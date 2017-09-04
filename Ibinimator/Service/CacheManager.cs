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
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using BitmapRenderTarget = SharpDX.Direct2D1.BitmapRenderTarget;
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
        private readonly Dictionary<ILayer, RectangleF> _bounds = new Dictionary<ILayer, RectangleF>();

        private readonly Dictionary<BrushInfo, (ILayer layer, Brush brush)> _brushBindings =
            new Dictionary<BrushInfo, (ILayer, Brush)>();

        private readonly Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();

        private readonly Dictionary<IFilledLayer, Brush> _fills = new Dictionary<IFilledLayer, Brush>();
        private readonly Dictionary<IStrokedLayer, (Brush brush, float width, StrokeStyle style)> _strokes =
            new Dictionary<IStrokedLayer, (Brush, float, StrokeStyle)>();

        private readonly Dictionary<IGeometricLayer, Geometry> _geometries = new Dictionary<IGeometricLayer, Geometry>();
        private readonly Dictionary<ITextLayer, TextLayout> _texts = new Dictionary<ITextLayer, TextLayout>();

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

        public Brush BindBrush(ILayer shape, BrushInfo brush)
        {
            if (brush == null) return null;

            var target = ArtView.RenderTarget;

            var fill = brush.ToDirectX(target);

            brush.PropertyChanged += OnBrushPropertyChanged;

            lock (_brushBindings) _brushBindings[brush] = (shape, fill);

            return fill;
        }

        public void BindLayer(ILayer layer)
        {
            var target = ArtView.RenderTarget;

            if (layer is IFilledLayer filled && filled.FillBrush != null)
                lock (_fills) _fills[filled] = BindBrush(filled, filled.FillBrush);

            if (layer is IStrokedLayer stroked && stroked.StrokeBrush != null)
            {
                lock (_strokes)
                {
                    if (stroked.StrokeStyle.DashStyle == DashStyle.Custom)
                        _strokes[stroked] =
                            (
                            BindBrush(stroked, stroked.StrokeBrush),
                            stroked.StrokeWidth,
                            new StrokeStyle1(
                                target.Factory.QueryInterface<Factory1>(),
                                stroked.StrokeStyle,
                                stroked.StrokeDashes.ToArray()
                            ));
                    else
                        _strokes[stroked] =
                            (
                            BindBrush(stroked, stroked.StrokeBrush),
                            stroked.StrokeWidth,
                            new StrokeStyle1(
                                target.Factory.QueryInterface<Factory1>(),
                                stroked.StrokeStyle
                            ));
                }
            }

            if (layer is IContainerLayer group)
            {
                foreach (var subLayer in group.SubLayers)
                    BindLayer(subLayer);

                group.LayerAdded +=
                    (sender, layer1) =>
                        BindLayer(layer1);
            }
        }

        public RectangleF GetAbsoluteBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.AbsoluteTransform);
        }

        public Bitmap GetBitmap(string key)
        {
            return _bitmaps[key];
        }

        public RectangleF GetBounds(ILayer layer)
        {
            return Get(_bounds, layer, l =>
            {
                if (l is Group g)
                    return g.SubLayers
                        .Select(GetRelativeBounds)
                        .Aggregate(RectangleF.Union);

                return l.GetBounds(this);
            });
        }

        public Brush GetBrush(string key)
        {
            return _brushes[key];
        }

        public Brush GetFill(IFilledLayer layer)
        {
            return Get(_fills, layer, l => l.FillBrush.ToDirectX(ArtView.RenderTarget));
        }

        public Geometry GetGeometry(IGeometricLayer layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(this));
        }

        public TextLayout GetTextLayout(ITextLayer text)
        {
            return Get(_texts, text, t => t.GetLayout(ArtView.DirectWriteFactory));
        }

        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public (Brush brush, float width, StrokeStyle style) GetStroke(IStrokedLayer layer)
        {
            return Get(_strokes, layer, l =>
            {
                var target = ArtView.RenderTarget;

                if (l.StrokeStyle.DashStyle == DashStyle.Solid)
                    return (
                        l.StrokeBrush?.ToDirectX(target),
                        l.StrokeWidth,
                        new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), l.StrokeStyle));

                return (
                    l.StrokeBrush?.ToDirectX(target),
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
            lock (_texts)
            {
                foreach (var (_, layout) in _texts.AsTuples())
                {
                    layout.Dispose();
                }

                _texts.Clear();
            }

            lock (_brushBindings)
            {
                foreach (var (brushInfo, (_, brush)) in _brushBindings.AsTuples())
                {
                    if (brushInfo != null)
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
            if (layer is IFilledLayer filled)
            {
                if (filled.FillBrush != null)
                {
                    lock (_fills)
                    {
                        if (_fills.TryGetValue(filled, out var fill))
                        {
                            fill?.Dispose();
                            _fills.Remove(filled);
                        }
                    }
                }
            }

            if(layer is IStrokedLayer stroked)
            {
                lock (_strokes)
                {
                    if (_strokes.TryGetValue(stroked, out var stroke))
                    {
                        stroke.brush?.Dispose();
                        stroke.style?.Dispose();
                        _strokes.Remove(stroked);
                    }
                }
            }

            if (layer is IGeometricLayer geometric)
            {
                lock (_geometries)
                {
                    if (_geometries.TryGetValue(geometric, out var geometry))
                    {
                        geometry.Dispose();
                        _geometries.Remove(geometric);
                    }
                }
            }

            lock (_bounds)
            {
                _bounds.Remove(layer);
            }

            if (layer is IContainerLayer group)
                foreach (var subLayer in group.SubLayers)
                    UnbindLayer(subLayer);
        }

        private static TV Get<TK, TV>(Dictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
        {
            lock (dict)
            {
                if (dict.TryGetValue(key, out var value) &&
                    (value as DisposeBase)?.IsDisposed != true &&
                    value != null)
                    return value;

                return dict[key] = fallback(key);
            }
        }

        private Bitmap LoadBitmap(RenderTarget target, string name)
        {
            var streamResourceInfo = Application
                .GetResourceStream(new Uri($"./Resources/Icon/{name}.png", UriKind.Relative));

            if (streamResourceInfo == null) return null;

            using (var stream = streamResourceInfo.Stream)
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
                        var bmp = new Bitmap(target, new Size2(sourceArea.Width, sourceArea.Height), temp,
                            data.Stride,
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

            ArtView.InvalidateSurface();
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = (Layer) sender;

            switch (e.PropertyName)
            {
                case "Geometry":
                    lock (_geometries)
                    {
                        if (layer is IGeometricLayer geom)
                        {
                            _geometries.TryGet(geom)?.Dispose();
                            _geometries[geom] = geom.GetGeometry(this);
                        }
                    }
                    goto case "Bounds";

                case nameof(IFilledLayer.FillBrush):
                    lock (_fills)
                    {
                        if (layer is IFilledLayer shape)
                        {
                            _fills.TryGet(shape)?.Dispose();
                            _fills[shape] = BindBrush(shape, shape.FillBrush);
                        }
                    }
                    break;

                case nameof(IStrokedLayer.StrokeBrush):
                    lock (_strokes)
                    {
                        if (layer is IStrokedLayer shape)
                        {
                            var stroke = _strokes.TryGet(shape);
                            stroke.brush?.Dispose();
                            stroke.brush = BindBrush(shape, shape.StrokeBrush);
                            _strokes[shape] = stroke;
                        }
                    }
                    break;

                case nameof(IStrokedLayer.StrokeWidth):
                    lock (_strokes)
                    {
                        if (layer is IStrokedLayer shape)
                        {
                            var stroke = _strokes.TryGet(shape);
                            stroke.width = shape.StrokeWidth;
                            _strokes[shape] = stroke;
                        }
                    }
                    break;

                case nameof(IStrokedLayer.StrokeStyle):
                case nameof(IStrokedLayer.StrokeDashes):
                    lock (_strokes)
                    {
                        if (layer is IStrokedLayer shape)
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
                    }
                    break;

                case nameof(ITextLayer.Value):
                case nameof(ITextLayer.FontSize):
                case nameof(ITextLayer.FontWeight):
                case nameof(ITextLayer.FontStyle):
                case nameof(ITextLayer.FontStretch):
                case nameof(ITextLayer.FontFamilyName):
                case "TextLayout":
                    lock (_texts)
                    {
                        if (layer is ITextLayer text)
                        {
                            _texts.TryGet(text)?.Dispose();
                            _texts[text] = text.GetLayout(ArtView.DirectWriteFactory);
                        }
                    }
                    goto case "Bounds";

                case "Bounds":
                    lock (_bounds)
                    {
                        if (layer is IContainerLayer g)
                            _bounds[layer] =
                                g.SubLayers
                                    .Select(GetRelativeBounds)
                                    .Aggregate(RectangleF.Union);
                        else
                            _bounds[layer] = layer.GetBounds(this);
                    }

                    if (layer.Parent != null)
                        OnLayerPropertyChanged(layer.Parent, new PropertyChangedEventArgs("Bounds"));
                    break;

                case nameof(Layer.Transform):
                    if (layer.Parent != null)
                        OnLayerPropertyChanged(layer.Parent, new PropertyChangedEventArgs("Bounds"));
                    break;
            }

            ArtView.InvalidateSurface();
        }
    }
}