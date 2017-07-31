using System.Collections.Generic;
using System;
using Ibinimator.Shared;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Collections;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class CacheManager : Model.Model, ICacheManager
    {
        #region Fields

        private Dictionary<string, Bitmap> _bitmaps = new Dictionary<string, Bitmap>();
        private Dictionary<Model.Layer, RectangleF> _bounds = new Dictionary<Model.Layer, RectangleF>();
        private Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        private Dictionary<Model.BrushInfo, (Model.Shape shape, Brush brush)> _brushBindings = 
            new Dictionary<Model.BrushInfo, (Model.Shape, Brush)>();
        private Dictionary<Model.Shape, Brush> _fills = new Dictionary<Model.Shape, Brush>();
        private Dictionary<Model.Shape, Geometry> _geometries = new Dictionary<Model.Shape, Geometry>();
        private Dictionary<Model.Shape, (Brush brush, float width, StrokeStyle style)> _strokes =
                            new Dictionary<Model.Shape, (Brush, float, StrokeStyle)>();

        #endregion Fields

        #region Constructors

        public CacheManager(ArtView artView)
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
            brush.PropertyChanged += OnBrushPropertyChanged;
            _brushBindings.Add(brush, (shape, fill));

            return fill;
        }

        private void OnBrushPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var brush = sender as Model.BrushInfo;
            var (shape, fill) = Get(_brushBindings, brush, k => (null, null));
            
            switch (e.PropertyName)
            {
                case "Opacity":
                    fill.Opacity = brush.Opacity;
                    break;

                case "Color":
                    (fill as SolidColorBrush).Color = brush.Color;
                    break;
            }

            InvalidateLayer(shape);
        }

        public void BindLayer(Model.Layer layer)
        {
            RenderTarget target = ArtView.RenderTarget;

            if (layer is Model.Shape shape)
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
            var layer = sender as Model.Layer;

            switch (e.PropertyName)
            {
                case nameof(Model.Layer.Transform):
                    _bounds[layer] = layer.GetAbsoluteBounds();
                    break;
                default:
                    break;
            }
        }

        public Bitmap GetBitmap(string key) => _bitmaps[key];

        public RectangleF GetBounds(Model.Layer layer) =>
            Get(_bounds, layer, l => l.GetAbsoluteBounds());

        public Brush GetBrush(string key) => _brushes[key];

        public Brush GetFill(Model.Shape layer) =>
            Get(_fills, layer, l => l.FillBrush.ToDirectX(ArtView.RenderTarget));

        public Geometry GetGeometry(Model.Shape layer) =>
            Get(_geometries, layer, l => l.GetGeometry(ArtView.RenderTarget.Factory));

        public (Brush brush, float width, StrokeStyle style) GetStroke(Model.Shape layer, RenderTarget target) =>
            Get(_strokes, layer, l => (
                l.StrokeBrush.ToDirectX(target),
                l.StrokeWidth,
                new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), l.StrokeStyle)));

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
            {
                if (entry.Value is System.Windows.Media.Color color)
                {
                    _brushes[entry.Key as string] =
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

        public async void UpdateLayer(Model.Layer layer, string property)
        {
            await Task.Run(() =>
            {
                lock (layer)
                {
                    Model.Shape shape = layer as Model.Shape;

                    switch (property)
                    {
                        case "Geometry":
                            _geometries.TryGetValue(shape, out Geometry geometry);
                            geometry?.Dispose();

                            if (shape != null)
                                _geometries[shape] = shape.GetGeometry(ArtView.RenderTarget.Factory);

                            InvalidateLayer(layer);
                            break;

                        case nameof(Model.Layer.Transform):
                            _bounds[layer] = layer.GetAbsoluteBounds();

                            InvalidateLayer(layer);
                            break;

                        case nameof(Model.Shape.FillBrush):
                            _fills.TryGetValue(shape, out Brush fill);
                            fill?.Dispose();
                            _fills[shape] = shape.FillBrush?.ToDirectX(ArtView.RenderTarget);
                            InvalidateLayer(layer);
                            break;

                        case nameof(Model.Shape.StrokeBrush):
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
            lock(dict)
            {
                if (dict.TryGetValue(key, out TV value)) return value;
                else return dict[key] = fallback(key);
            }
        }

        private void InvalidateLayer(Model.Layer layer)
        {
            ArtView.InvalidateSurface();
        }

        private unsafe Bitmap LoadBitmap(RenderTarget target, string name)
        {
            using (var stream = App.GetResourceStream(new Uri($"./Resources/Icon/{name}.png", UriKind.Relative)).Stream)
            {
                using (var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream))
                {
                    var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var bitmapProperties = new BitmapProperties(
                        new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                            target.DotsPerInch.Width, target.DotsPerInch.Height);

                    var data = bitmap.LockBits(sourceArea,
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                    var dpi = target.DotsPerInch.Width / 96;

                    using (var temp = new DataStream(data.Scan0, bitmap.Width * sizeof(int), true, true))
                    {
                        var bmp = new Bitmap(target, new Size2(sourceArea.Width, sourceArea.Height), temp, data.Stride, bitmapProperties);

                        bitmap.UnlockBits(data);

                        return bmp;
                    }
                }
            }
        }

        #endregion Methods
    }
}