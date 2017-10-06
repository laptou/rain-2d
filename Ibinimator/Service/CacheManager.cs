using System.Collections.Generic;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Imaging;
using Ibinimator.Utility;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Factory1 = SharpDX.Direct2D1.Factory1;
using Format = SharpDX.DXGI.Format;
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

        private readonly Dictionary<IGeometricLayer, Geometry> _geometries = new Dictionary<IGeometricLayer, Geometry>()
            ;

        private readonly Dictionary<IGeometricLayer, GeometryRealization> _geometryRealizations =
            new Dictionary<IGeometricLayer, GeometryRealization>();

        private readonly Dictionary<(ILayer layer, int id), IDisposable> _resources =
            new Dictionary<(ILayer layer, int id), IDisposable>();

        private readonly Dictionary<StrokeInfo, (IStrokedLayer layer, Stroke stroke)> _strokeBindings =
            new Dictionary<StrokeInfo, (IStrokedLayer layer, Stroke stroke)>();

        private readonly Dictionary<IStrokedLayer, Stroke> _strokes = new Dictionary<IStrokedLayer, Stroke>();
        private readonly Dictionary<ITextLayer, TextLayout> _texts = new Dictionary<ITextLayer, TextLayout>();

        public CacheManager(ArtView artView)
        {
            ArtView = artView;
        }

        public Stroke BindStroke(IStrokedLayer layer, StrokeInfo info)
        {
            if (info == null) return default;

            var target = ArtView.RenderTarget;

            info.PropertyChanged += OnStrokePropertyChanged;

            StrokeStyle1 style;

            if (info.Style.DashStyle == DashStyle.Solid || info.Dashes.Count == 0)
                style = new StrokeStyle1(
                    target.Factory.QueryInterface<Factory1>(),
                    info.Style);
            else
                style = new StrokeStyle1(
                    target.Factory.QueryInterface<Factory1>(),
                    info.Style,
                    info.Dashes.ToArray());

            var stroke = new Stroke
            {
                Style = style,
                Width = info.Width,
                Brush = BindBrush(layer, layer.StrokeBrush)
            };

            lock (_strokeBindings)
            {
                _strokeBindings[info] = (layer, stroke);
            }

            return stroke;
        }

        public void UnbindLayer(Layer layer)
        {
            if (layer is IFilledLayer filled)
                if (filled.FillBrush != null)
                    lock (_fills)
                    {
                        if (_fills.TryGetValue(filled, out var fill))
                        {
                            fill?.Dispose();
                            _fills.Remove(filled);
                        }
                    }

            if (layer is IStrokedLayer stroked)
                lock (_strokes)
                {
                    if (_strokes.TryGetValue(stroked, out var stroke))
                    {
                        stroke.Dispose();
                        _strokes.Remove(stroked);
                    }
                }

            if (layer is IGeometricLayer geometric)
                lock (_geometries)
                {
                    if (_geometries.TryGetValue(geometric, out var geometry))
                    {
                        geometry.Dispose();
                        _geometries.Remove(geometric);
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

            if (fill == null) return;

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
                            lock (layer)
                            {
                                _geometries.TryGet(geom)?.Dispose();
                                var geometry = _geometries[geom] = geom.GetGeometry(this);

                                var ctx = ArtView.RenderTarget.QueryInterface<DeviceContext1>();

                                if (geometry != null)
                                {
                                    _geometryRealizations.TryGet(geom)?.Dispose();
                                    _geometryRealizations[geom] =
                                        new GeometryRealization(ctx, geometry, geometry.FlatteningTolerance);
                                }
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
                case nameof(IStrokedLayer.StrokeInfo):
                    lock (_strokes)
                    {
                        if (layer is IStrokedLayer shape)
                        {
                            var stroke = _strokes.TryGet(shape);

                            if (stroke != null)
                                lock (stroke)
                                {
                                    stroke.Dispose();
                                }

                            GetStroke(shape); // GetStroke repopulates it
                        }
                    }
                    break;

                case "TextLayout":
                    lock (_texts)
                    {
                        if (layer is ITextLayer text)
                        {
                            _texts.TryGet(text)?.Dispose();
                            _texts[text] = text.GetLayout(ArtView.DirectWriteFactory);
                        }
                    }
                    goto case "Geometry";

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
                    goto case nameof(Layer.Transform);

                case nameof(Layer.Transform):
                    if (layer.Parent != null)
                        OnLayerPropertyChanged(layer.Parent, new PropertyChangedEventArgs("Bounds"));
                    break;
            }

            ArtView.InvalidateSurface();
        }

        private void OnStrokePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var info = (StrokeInfo) sender;
            if (info == null) return;

            var (layer, stroke) = Get(_strokeBindings, info, k => (null, default));

            if (stroke == null)
            {
                BindStroke(layer, info);
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(StrokeInfo.Width):
                    stroke.Width = info.Width;
                    break;

                case nameof(StrokeInfo.Style):
                case nameof(StrokeInfo.Dashes):
                    stroke.Style.Dispose();

                    if (info.Style.DashStyle == DashStyle.Solid)
                        stroke.Style = new StrokeStyle1(
                            ArtView.Direct2DFactory.QueryInterface<Factory1>(),
                            info.Style);
                    else
                        stroke.Style = new StrokeStyle1(
                            ArtView.Direct2DFactory.QueryInterface<Factory1>(),
                            info.Style,
                            info.Dashes.ToArray());
                    break;
            }

            ArtView.InvalidateSurface();
        }

        #region ICacheManager Members

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

            lock (_brushBindings)
            {
                _brushBindings[brush] = (shape, fill);
            }

            return fill;
        }

        public void BindLayer(ILayer layer)
        {
            var target = ArtView.RenderTarget;

            if (layer is IFilledLayer filled && filled.FillBrush != null)
                lock (_fills)
                {
                    _fills.TryGet(filled)?.Dispose();
                    _fills[filled] = BindBrush(filled, filled.FillBrush);
                }

            if (layer is IStrokedLayer stroked && stroked.StrokeBrush != null)
                lock (_strokes)
                {
                    _strokes.TryGet(stroked)?.Dispose();
                    _strokes[stroked] = BindStroke(stroked, stroked.StrokeInfo);
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

        public bool ClearResource(ILayer layer, int id)
        {
            lock (_resources)
            {
                _resources.TryGet((layer, id))?.Dispose();
                return _resources.Remove((layer, id));
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
            return Get(_bounds, layer, l => l.GetBounds(this));
        }

        public Brush GetBrush(string key)
        {
            return _brushes[key];
        }

        public Brush GetFill(IFilledLayer layer)
        {
            return Get(_fills, layer, l => BindBrush(l, l.FillBrush));
        }

        public Geometry GetGeometry(IGeometricLayer layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(this));
        }

        public GeometryRealization GetGeometryRealization(IGeometricLayer layer)
        {
            return Get(_geometryRealizations, layer, l =>
            {
                var ctx = ArtView.RenderTarget.QueryInterface<DeviceContext1>();
                var geom = GetGeometry(l);
                return new GeometryRealization(ctx, geom, geom.FlatteningTolerance);
            });
        }

        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public T GetResource<T>(ILayer layer, int id) where T : IDisposable
        {
            return (T) Get(_resources, (layer, id), l => l.Item1.GetResource(this, id));
        }

        public IEnumerable<(int id, T resource)> GetResources<T>(ILayer layer) where T : IDisposable
        {
            lock (_resources)
            {
                return _resources.Where(kv => kv.Key.layer == layer)
                    .Select(kv => (kv.Key.id, (T) kv.Value))
                    .ToArray();
            }
        }

        public Stroke GetStroke(IStrokedLayer layer)
        {
            return Get(_strokes, layer, l => BindStroke(l, l.StrokeInfo));
        }

        public TextLayout GetTextLayout(ITextLayer text)
        {
            return Get(_texts, text, t => t.GetLayout(ArtView.DirectWriteFactory));
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
                if (entry.Value is System.Windows.Media.Color color)
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
            ResetDeviceResources();

            lock (_geometries)
            {
                foreach (var (_, geometry) in _geometries.AsTuples()) geometry?.Dispose();
                _geometries.Clear();
            }

            lock (_texts)
            {
                foreach (var (_, layout) in _texts.AsTuples())
                    layout.Dispose();

                _texts.Clear();
            }

            lock (_resources)
            {
                foreach (var (_, resource) in _resources.AsTuples()) resource?.Dispose();
                _resources.Clear();
            }
        }

        public void ResetDeviceResources()
        {
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

            lock (_strokeBindings)
            {
                foreach (var (strokeInfo, (_, stroke)) in _strokeBindings.AsTuples())
                {
                    if (strokeInfo != null)
                        strokeInfo.PropertyChanged -= OnStrokePropertyChanged;

                    stroke.Dispose();
                }
                _strokeBindings.Clear();
            }

            lock (_geometryRealizations)
            {
                foreach (var geometry in _geometryRealizations.Values) geometry?.Dispose();
                _geometryRealizations.Clear();
            }

            lock (_fills)
            {
                foreach (var (_, fill) in _fills.AsTuples()) fill?.Dispose();
                _fills.Clear();
            }

            lock (_strokes)
            {
                foreach (var (_, stroke) in _strokes.AsTuples()) stroke.Dispose();
                _strokes.Clear();
            }
        }

        public void SetResource<T>(ILayer layer, int id, T resource) where T : IDisposable
        {
            lock (_resources)
            {
                _resources.TryGet((layer, id))?.Dispose();
                _resources[(layer, id)] = resource;
            }
        }

        public ArtView ArtView { get; set; }

        #endregion
    }
}