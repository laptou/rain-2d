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
using Factory1 = SharpDX.Direct2D1.Factory1;
using Image = System.Drawing.Image;
using Layer = Ibinimator.Model.Layer;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace Ibinimator.Service
{
    public class CacheManager : Model.Model, ICacheManager
    {
        #region Constructors

        public CacheManager(ArtView artView)
        {
            ArtView = artView;
        }

        #endregion Constructors

        #region Properties

        public ArtView ArtView { get; set; }

        #endregion Properties

        #region Fields

        private readonly Dictionary<string, Bitmap> _bitmaps = new Dictionary<string, Bitmap>();
        private readonly Dictionary<Layer, RectangleF> _bounds = new Dictionary<Layer, RectangleF>();
        private readonly Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();

        private readonly Dictionary<BrushInfo, (Shape shape, Brush brush)> _brushBindings =
            new Dictionary<BrushInfo, (Shape, Brush)>();

        private readonly Dictionary<Shape, Brush> _fills = new Dictionary<Shape, Brush>();
        private readonly Dictionary<Shape, Geometry> _geometries = new Dictionary<Shape, Geometry>();

        private readonly Dictionary<Shape, (Brush brush, float width, StrokeStyle style)> _strokes =
            new Dictionary<Shape, (Brush, float, StrokeStyle)>();

        #endregion Fields

        #region Methods

        public Brush BindBrush(Shape shape, BrushInfo brush)
        {
            if (brush == null) return null;

            var target = ArtView.RenderTarget;

            var fill = brush.ToDirectX(target);
            brush.PropertyChanged += OnBrushPropertyChanged;
            shape.PropertyChanged += OnShapePropertyChanged;

            lock (_brushBindings)
            {
                _brushBindings.Add(brush, (shape, fill));
            }

            return fill;
        }

        private void OnShapePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var shape = (Shape) sender;

            switch (e.PropertyName)
            {
                case "Geometry":
                    lock (_geometries)
                    {
                        _geometries[shape]?.Dispose();
                        _geometries[shape] = shape.GetGeometry(ArtView.RenderTarget.Factory);
                    }
                    break;

                case nameof(Shape.FillBrush):
                    lock (_fills)
                    {
                        _fills[shape]?.Dispose();
                        _fills[shape] = shape.FillBrush.ToDirectX(ArtView.RenderTarget);
                    }
                    break;

                case nameof(Shape.StrokeBrush):
                    lock (_strokes)
                    {
                        var stroke = _strokes[shape];
                        stroke.brush?.Dispose();
                        stroke.brush = shape.StrokeBrush.ToDirectX(ArtView.RenderTarget);
                        _strokes[shape] = stroke;
                    }
                    break;

                case nameof(Shape.StrokeWidth):
                    lock (_strokes)
                    {
                        var stroke = _strokes[shape];
                        stroke.width = shape.StrokeWidth;
                        _strokes[shape] = stroke;
                    }
                    break;

                case nameof(Shape.StrokeStyle):
                    lock (_strokes)
                    {
                        var stroke = _strokes[shape];
                        stroke.style.Dispose();
                        stroke.style = new StrokeStyle1(ArtView.RenderTarget.Factory.QueryInterface<Factory1>(),
                            shape.StrokeStyle);
                        _strokes[shape] = stroke;
                    }
                    break;
            }

            InvalidateLayer(shape);
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

        public void BindLayer(Layer layer)
        {
            var target = ArtView.RenderTarget;

            if (layer is Shape shape)
            {
                if (shape.FillBrush != null)
                    _fills[shape] = BindBrush(shape, shape.FillBrush);
                if (shape.StrokeBrush != null)
                    _strokes[shape] = (
                        BindBrush(shape, shape.StrokeBrush),
                        shape.StrokeWidth,
                        new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), shape.StrokeStyle)
                        );
            }

            layer.PropertyChanged += OnLayerPropertyChanged;

            foreach (var subLayer in layer.SubLayers)
                BindLayer(subLayer);
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var layer = sender as Layer;

            switch (e.PropertyName)
            {
                case nameof(Layer.Transform):
                    _bounds[layer] = layer.GetAbsoluteBounds();
                    break;
            }
        }

        public Bitmap GetBitmap(string key)
        {
            return _bitmaps[key];
        }

        public RectangleF GetBounds(Layer layer)
        {
            return Get(_bounds, layer, l => l.GetAbsoluteBounds());
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

        public (Brush brush, float width, StrokeStyle style) GetStroke(Shape layer, RenderTarget target)
        {
            return Get(_strokes, layer, l => (
                l.StrokeBrush.ToDirectX(target),
                l.StrokeWidth,
                new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), l.StrokeStyle)));
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
                    _brushes[entry.Key as string] =
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
                foreach (var (name, brush) in _brushes.AsTuples())
                    brush?.Dispose();
                _brushes.Clear();
            }

            lock (_bitmaps)
            {
                foreach (var (name, bitmap) in _bitmaps.AsTuples()) bitmap?.Dispose();
                _bitmaps.Clear();
            }
        }

        public void ResetLayerCache()
        {
            lock (_brushBindings)
            {
                foreach (var (brushInfo, (shape, brush)) in _brushBindings.AsTuples())
                {
                    brushInfo.PropertyChanged -= OnBrushPropertyChanged;
                    brush?.Dispose();
                }
                _brushBindings.Clear();
            }

            lock (_geometries)
            {
                foreach (var (layer, geometry) in _geometries.AsTuples()) geometry?.Dispose();
                _geometries.Clear();
            }

            lock (_fills)
            {
                foreach (var (layer, fill) in _fills.AsTuples()) fill?.Dispose();
                _fills.Clear();
            }

            lock (_strokes)
            {
                foreach (var (layer, (stroke, width, style)) in _strokes.AsTuples())
                {
                    stroke?.Dispose();
                    style?.Dispose();
                }
                _strokes.Clear();
            }
        }

        public async void UpdateLayer(Layer layer, string property)
        {
            await Task.Run(() =>
            {
                lock (layer)
                {
                    var shape = layer as Shape;

                    switch (property)
                    {
                        case "Geometry":
                            _geometries.TryGetValue(shape, out Geometry geometry);
                            geometry?.Dispose();

                            if (shape != null)
                                _geometries[shape] = shape.GetGeometry(ArtView.RenderTarget.Factory);

                            InvalidateLayer(layer);
                            break;

                        case nameof(Layer.Transform):
                            _bounds[layer] = layer.GetAbsoluteBounds();

                            InvalidateLayer(layer);
                            break;

                        case nameof(Shape.FillBrush):
                            _fills.TryGetValue(shape, out Brush fill);
                            fill?.Dispose();
                            _fills[shape] = shape.FillBrush?.ToDirectX(ArtView.RenderTarget);
                            InvalidateLayer(layer);
                            break;

                        case nameof(Shape.StrokeBrush):
                            _strokes.TryGetValue(shape, out (Brush brush, float, StrokeStyle) stroke);
                            stroke.brush?.Dispose();
                            stroke.brush = shape.StrokeBrush?.ToDirectX(ArtView.RenderTarget);
                            InvalidateLayer(layer);
                            break;

                        default:
                            break;
                    }
                }
            });
        }

        private TV Get<TK, TV>(Dictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
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
            using (var stream = Application.GetResourceStream(new Uri($"./Resources/Icon/{name}.png", UriKind.Relative))
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

        #endregion Methods
    }
}