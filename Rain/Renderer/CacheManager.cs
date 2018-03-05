using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

using Rain.Core.Utility;

using System.Threading.Tasks;
using System.Windows;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Resources;

namespace Rain.Renderer
{
    public class CacheManager : Core.Model.Model, ICacheManager
    {
        private readonly Dictionary<string, IRenderImage> _bitmaps =
            new Dictionary<string, IRenderImage>();

        private readonly Dictionary<ILayer, RectangleF> _bounds =
            new Dictionary<ILayer, RectangleF>();

        private readonly Dictionary<IBrushInfo, (ILayer layer, IBrush brush)> _brushBindings =
            new Dictionary<IBrushInfo, (ILayer, IBrush)>();

        private readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();

        private readonly Dictionary<IFilledLayer, IBrush> _fills =
            new Dictionary<IFilledLayer, IBrush>();

        private readonly Dictionary<IGeometricLayer, IGeometry> _geometries =
            new Dictionary<IGeometricLayer, IGeometry>();

        private readonly Dictionary<IImageLayer, IRenderImage> _images =
            new Dictionary<IImageLayer, IRenderImage>();

        private readonly ReaderWriterLockSlim _renderLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly Dictionary<IPenInfo, (IStrokedLayer layer, IPen stroke)> _strokeBindings =
            new Dictionary<IPenInfo, (IStrokedLayer layer, IPen stroke)>();

        private readonly Dictionary<IStrokedLayer, IPen> _strokes =
            new Dictionary<IStrokedLayer, IPen>();

        private readonly Dictionary<ITextLayer, ITextLayout> _texts =
            new Dictionary<ITextLayer, ITextLayout>();

        private bool _suppressed;

        public CacheManager(IArtContext context) { Context = context; }

        public IBrush BindBrush(ILayer shape, IBrushInfo brush)
        {
            if (brush == null) return null;

            var fill = brush.CreateBrush(Context.RenderContext);

            brush.PropertyChanged += OnBrushPropertyChanged;

            lock (_brushBindings)
            {
                _brushBindings[brush] = (shape, fill);
            }

            return fill;
        }

        public IPen BindStroke(IStrokedLayer layer, IPenInfo info)
        {
            if (info == null) return default;

            info.PropertyChanged += OnStrokePropertyChanged;

            var pen = info.CreatePen(Context.RenderContext);

            lock (_strokeBindings)
            {
                _strokeBindings[info] = (layer, pen);
            }

            return pen;
        }

        public void EnterReadLock() { _renderLock.EnterReadLock(); }

        public void EnterWriteLock() { _renderLock.EnterWriteLock(); }

        public void ExitReadLock() { _renderLock.ExitReadLock(); }

        public void ExitWriteLock() { _renderLock.ExitWriteLock(); }

        private TV Get<TK, TV>(IDictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
        {
            EnterReadLock();

            var exists = dict.TryGetValue(key, out var value) && value != null;
            var valid = (value as ResourceBase)?.Disposed != true;

            ExitReadLock();

            if (exists && valid)
                return value;

            EnterWriteLock();

            var newVal = dict[key] = fallback(key);

            ExitWriteLock();

            return newVal;
        }

        private IRenderImage LoadBitmap(RenderContext target, string name)
        {
            var uri = new Uri($"./Resources/Icon/{name}.png", UriKind.Relative);


            if (target.GetDpi() > 96)
                uri = new Uri($"./Resources/Icon/{name}@2x.png", UriKind.Relative);

            using (var stream = Application.GetResourceStream(uri)?.Stream)
            {
                using (var img = Context.ResourceContext.LoadImageFromStream(stream))
                {
                    var bmp = Context.RenderContext.GetRenderImage(img.Frames[0]);

                    return bmp;
                }
            }
        }

        private void OnBrushPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var info = (IBrushInfo) sender;
            var (shape, fill) = Get(_brushBindings, info, k => (null, null));

            if (fill == null) return;

            GradientBrushInfo gradInfo;

            switch (fill)
            {
                case IBrush _ when e.PropertyName == nameof(BrushInfo.Opacity):
                    fill.Opacity = info.Opacity;

                    break;
                case IBrush _ when e.PropertyName == nameof(BrushInfo.Transform):
                    fill.Transform = info.Transform;

                    break;
                case ISolidColorBrush solid
                    when e.PropertyName == nameof(SolidColorBrushInfo.Color):
                    var solidInfo = (SolidColorBrushInfo) info;
                    solid.Color = solidInfo.Color;

                    break;
                case IGradientBrush grad when e.PropertyName == nameof(GradientBrushInfo.Stops):
                    gradInfo = (GradientBrushInfo) info;
                    grad.Stops.ReplaceRange(gradInfo.Stops);

                    break;
                case IGradientBrush grad when e.PropertyName == nameof(GradientBrushInfo.Type):
                    // let the brush be recreated on the next frame
                    grad.Dispose();
                    _brushBindings.Remove(info);
                    break;
                case ILinearGradientBrush grad
                    when e.PropertyName == nameof(GradientBrushInfo.StartPoint):
                    gradInfo = (GradientBrushInfo) info;
                    grad.StartX = gradInfo.StartPoint.X;
                    grad.StartY = gradInfo.StartPoint.Y;

                    break;
                case ILinearGradientBrush grad
                    when e.PropertyName == nameof(GradientBrushInfo.EndPoint):
                    gradInfo = (GradientBrushInfo) info;
                    grad.EndX = gradInfo.EndPoint.X;
                    grad.EndY = gradInfo.EndPoint.Y;

                    break;
                case IRadialGradientBrush grad
                    when e.PropertyName == nameof(GradientBrushInfo.StartPoint):
                    gradInfo = (GradientBrushInfo) info;
                    grad.CenterX = gradInfo.StartPoint.X;
                    grad.CenterY = gradInfo.StartPoint.Y;

                    break;
                case IRadialGradientBrush grad
                    when e.PropertyName == nameof(GradientBrushInfo.EndPoint):
                    gradInfo = (GradientBrushInfo) info;
                    grad.RadiusX = gradInfo.EndPoint.X - gradInfo.StartPoint.X;
                    grad.RadiusY = gradInfo.EndPoint.Y - gradInfo.StartPoint.Y;

                    break;
            }

            if (_suppressed) return;

            Context.InvalidateRender();
        }

        private void OnManagerDetached(object sender, EventArgs e)
        {
            if (sender is IViewManager)
                ReleaseSceneResources();
        }

        private void OnStrokePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var info = (IPenInfo) sender;

            if (info == null) return;

            var (layer, stroke) = Get(_strokeBindings, info, k => (null, default));

            if (stroke == null)
            {
                BindStroke(layer, info);

                return;
            }

            switch (e.PropertyName)
            {
                case nameof(IPenInfo.Width):
                    stroke.Width = info.Width;

                    break;

                case nameof(IPenInfo.Dashes):
                case nameof(IPenInfo.HasDashes):
                    stroke.Dashes.Clear();
                    if (info.HasDashes)
                        foreach (var dash in info.Dashes)
                            stroke.Dashes.Add(dash);

                    break;

                case nameof(IPenInfo.DashOffset):
                    stroke.DashOffset = info.DashOffset;

                    break;


                case nameof(IPenInfo.LineCap):
                    stroke.LineCap = info.LineCap;

                    break;

                case nameof(IPenInfo.LineJoin):
                    stroke.LineJoin = info.LineJoin;

                    break;

                case nameof(IPenInfo.MiterLimit):
                    stroke.MiterLimit = info.MiterLimit;

                    break;
            }

            if (_suppressed) return;

            Context.InvalidateRender();
        }

        #region ICacheManager Members

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            context.ManagerDetached += OnManagerDetached;
            context.RaiseAttached(this);
        }

        /// <inheritdoc />
        public void BindLayer(ILayer layer)
        {
            EnterWriteLock();

            void BindProperty<K, V>(Action<EventHandler> adder, Dictionary<K, V> d)
                where K : ILayer where V : IDisposable
            {
                adder((s, e) =>
                      {
                          EnterWriteLock();

                          if (s is K t)
                              d.TryGet(t)?.Dispose();

                          ExitWriteLock();

                          if (_suppressed) return;

                          Context.InvalidateRender();
                      });
            }

            if (layer is IFilledLayer filled)
            {
                lock (_fills)
                {
                    _fills.TryGet(filled)?.Dispose();
                    _fills[filled] = BindBrush(filled, filled.Fill);
                }
                
                BindProperty(h => filled.FillChanged += h, _fills);
            }

            if (layer is IStrokedLayer stroked)
            {
                lock (_strokes)
                {
                    _strokes.TryGet(stroked)?.Dispose();
                    _strokes[stroked] = BindStroke(stroked, stroked.Stroke);
                }

                BindProperty(h => stroked.StrokeChanged += h, _strokes);

            }

            if (layer is ITextLayer text)
                BindProperty(h => text.LayoutChanged += h, _texts);

            if (layer is IImageLayer image)
                BindProperty(h => image.ImageChanged += h, _images);

            if (layer is IGeometricLayer geometric)
                BindProperty(h => geometric.GeometryChanged += h, _geometries);

            layer.BoundsChanged += (s, e) =>
                                   {
                                       if (s is ILayer l)
                                       {
                                           EnterWriteLock();
                                           _bounds[l] = l.GetBounds(Context);
                                           ExitWriteLock();
                                       }

                                       if (_suppressed) return;

                                       Context.InvalidateRender();
                                   };

            if (layer is IContainerLayer group)
            {
                foreach (var subLayer in group.SubLayers)
                    BindLayer(subLayer);

                group.LayerAdded += (sender, layer1) => BindLayer(layer1);
                group.LayerRemoved += (sender, layer1) => UnbindLayer(layer1);
            }

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            ReleaseResources();
            context.ManagerDetached -= OnManagerDetached;
            context.RaiseDetached(this);
        }

        /// <inheritdoc />
        public RectangleF GetAbsoluteBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.AbsoluteTransform);
        }

        public IRenderImage GetBitmap(string key) { return _bitmaps[key]; }

        /// <inheritdoc />
        public RectangleF GetBounds(ILayer layer)
        {
            return Get(_bounds, layer, l => l.GetBounds(Context));
        }

        public IBrush GetBrush(string key) { return _brushes[key]; }

        public IBrush GetFill(IFilledLayer layer)
        {
            return Get(_fills, layer, l => BindBrush(l, l.Fill));
        }

        public IGeometry GetGeometry(IGeometricLayer layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(Context));
        }

        public IRenderImage GetImage(IImageLayer layer)
        {
            return Get(_images, layer, t => t.GetImage(Context));
        }

        /// <inheritdoc />
        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public IPen GetStroke(IStrokedLayer layer)
        {
            return Get(_strokes, layer, l => BindStroke(l, l.Stroke));
        }

        public ITextLayout GetTextLayout(ITextLayer layer)
        {
            return Get(_texts, layer, t => t.GetLayout(Context));
        }

        public void LoadBitmaps(RenderContext target)
        {
            _bitmaps["cursor-resize-ns"] = LoadBitmap(target, "cursor-resize-ns");
            _bitmaps["cursor-resize-ew"] = LoadBitmap(target, "cursor-resize-ew");
            _bitmaps["cursor-resize-nwse"] = LoadBitmap(target, "cursor-resize-nwse");
            _bitmaps["cursor-resize-nesw"] = LoadBitmap(target, "cursor-resize-nesw");
            _bitmaps["cursor-rotate"] = LoadBitmap(target, "cursor-rotate");
        }

        public void LoadBrushes(RenderContext target)
        {
            foreach (var field in typeof(EditorColors).GetFields())
                _brushes[field.Name] = target.CreateBrush((Color) field.GetValue(null));
        }

        public void ReleaseDeviceResources()
        {
            EnterWriteLock();

            foreach (var (_, brush) in _brushes.AsTuples())
                brush?.Dispose();
            _brushes.Clear();

            foreach (var (_, bitmap) in _bitmaps.AsTuples()) bitmap?.Dispose();
            _bitmaps.Clear();


            foreach (var (brushInfo, (_, brush)) in _brushBindings.AsTuples())
            {
                if (brushInfo != null)
                    brushInfo.PropertyChanged -= OnBrushPropertyChanged;
                brush?.Dispose();
            }

            _brushBindings.Clear();

            foreach (var (strokeInfo, (_, stroke)) in _strokeBindings.AsTuples())
            {
                if (strokeInfo != null)
                    strokeInfo.PropertyChanged -= OnStrokePropertyChanged;

                stroke.Dispose();
            }

            _strokeBindings.Clear();

            foreach (var (_, fill) in _fills.AsTuples()) fill?.Dispose();
            _fills.Clear();

            foreach (var (_, stroke) in _strokes.AsTuples()) stroke?.Dispose();
            _strokes.Clear();

            foreach (var (_, image) in _images.AsTuples()) image?.Dispose();
            _images.Clear();

            ExitWriteLock();
        }

        public void ReleaseResources()
        {
            ReleaseDeviceResources();

            EnterWriteLock();

            foreach (var (_, geometry) in _geometries.AsTuples()) geometry?.Dispose();
            _geometries.Clear();

            foreach (var (_, layout) in _texts.AsTuples())
                layout.Dispose();

            _texts.Clear();

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void ReleaseSceneResources()
        {
            EnterWriteLock();

            foreach (var (brushInfo, (_, brush)) in _brushBindings.AsTuples())
            {
                if (brushInfo != null)
                    brushInfo.PropertyChanged -= OnBrushPropertyChanged;

                brush?.Dispose();
            }

            _brushBindings.Clear();

            foreach (var (strokeInfo, (_, stroke)) in _strokeBindings.AsTuples())
            {
                if (strokeInfo != null)
                    strokeInfo.PropertyChanged -= OnStrokePropertyChanged;

                stroke.Dispose();
            }

            _strokeBindings.Clear();

            foreach (var (_, fill) in _fills.AsTuples()) fill?.Dispose();
            _fills.Clear();

            foreach (var (_, stroke) in _strokes.AsTuples()) stroke?.Dispose();
            _strokes.Clear();

            foreach (var (_, geometry) in _geometries.AsTuples()) geometry?.Dispose();
            _geometries.Clear();

            foreach (var (_, layout) in _texts.AsTuples()) layout?.Dispose();
            _texts.Clear();

            foreach (var (_, image) in _images.AsTuples()) image?.Dispose();
            _images.Clear();

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void RestoreInvalidation() { _suppressed = true; }

        /// <inheritdoc />
        public void SuppressInvalidation() { _suppressed = true; }

        /// <inheritdoc />
        public void UnbindLayer(ILayer layer)
        {
            EnterWriteLock();

            if (layer is IFilledLayer filled)
                if (filled.Fill != null)
                    if (_fills.TryGetValue(filled, out var fill))
                    {
                        fill?.Dispose();
                        _fills.Remove(filled);
                    }

            if (layer is IStrokedLayer stroked)
                if (stroked.Stroke != null)
                    if (_strokes.TryGetValue(stroked, out var stroke))
                    {
                        stroke.Dispose();
                        _strokes.Remove(stroked);
                    }

            if (layer is IGeometricLayer geometric)
                if (_geometries.TryGetValue(geometric, out var geometry))
                {
                    geometry.Dispose();
                    _geometries.Remove(geometric);
                }

            lock (_bounds)
            {
                _bounds.Remove(layer);
            }

            ExitWriteLock();

            if (layer is IContainerLayer group)
                foreach (var subLayer in group.SubLayers)
                    UnbindLayer(subLayer);
        }

        public IArtContext Context { get; set; }

        #endregion
    }
}