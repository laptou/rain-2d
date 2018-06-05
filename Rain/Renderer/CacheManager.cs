using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

using System.Threading.Tasks;
using System.Windows;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Service;

namespace Rain.Renderer
{
    public class CacheManager : Core.Model.Model, ICacheManager
    {
        private readonly ReaderWriterLockSlim _renderLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool  _suppressed;
        private Timer _timer;

        public CacheManager(IArtContext context) { Context = context; }

        public IBrush BindBrush(IBrushInfo fill)
        {
            if (fill == null) return null;

            var brush = fill.CreateBrush(Context.RenderContext);
            var disposable = new CompositeDisposable();

            disposable.Add(fill.CreateOpacityObservable()
                               .Subscribe(opacity =>
                                          {
                                              brush.Opacity = opacity;
                                              Invalidate();
                                          }));

            disposable.Add(fill.CreateTransformObservable()
                               .Subscribe(transform =>
                                          {
                                              brush.Transform = transform;
                                              Invalidate();
                                          }));

            if (fill is ISolidColorBrushInfo color &&
                brush is ISolidColorBrush colorBrush)
                disposable.Add(color.CreateColorObservable()
                                    .Subscribe(c =>
                                               {
                                                   colorBrush.Color = c;
                                                   Invalidate();
                                               }));

            if (fill is IGradientBrushInfo gradient &&
                brush is IGradientBrush gradientBrush)
            {
                disposable.Add(gradient.CreateStopsObservable()
                                       .Subscribe(stops =>
                                                  {
                                                      gradientBrush.Stops.ReplaceRange(stops);
                                                      Invalidate();
                                                  }));

                if (brush is ILinearGradientBrush linear)
                {
                    disposable.Add(gradient.CreateStartPointObservable()
                                           .Subscribe(start =>
                                                      {
                                                          (linear.StartX, linear.StartY) = start;
                                                          Invalidate();
                                                      }));

                    disposable.Add(gradient.CreateEndPointObservable()
                                           .Subscribe(end =>
                                                      {
                                                          (linear.EndX, linear.EndY) = end;
                                                          Invalidate();
                                                      }));
                }

                if (brush is IRadialGradientBrush radial)
                {
                    disposable.Add(gradient.CreateStartPointObservable()
                                           .Subscribe(start =>
                                                      {
                                                          (radial.CenterX, radial.CenterY) = start;
                                                          Invalidate();
                                                      }));

                    disposable.Add(gradient.CreateEndPointObservable()
                                           .Subscribe(end =>
                                                      {
                                                          (radial.RadiusX, radial.RadiusY) = end - gradient.StartPoint;
                                                          Invalidate();
                                                      }));
                }
            }

            brush.Disposed += (s, e) => disposable.Dispose();

            return brush;
        }

        public IPen BindStroke(IPenInfo info)
        {
            if (info == null) return default;

            var pen = info.CreatePen(Context.RenderContext);
            var disposable = new CompositeDisposable();

            disposable.Add(info.CreateBrushObservable()
                               .Subscribe(brush =>
                                          {
                                              pen.Brush = BindBrush(brush);
                                              
Invalidate();
                                          }));

            disposable.Add(info.CreateWidthObservable()
                               .Subscribe(width =>
                                          {
                                              pen.Width = width;
                                              Invalidate();
                                          }));

            disposable.Add(info.CreateDashesObservable()
                               .Subscribe(dashes =>
                                          {
                                              pen.Dashes.ReplaceAll(dashes);
                                              Invalidate();
                                          }));

            disposable.Add(info.CreateDashOffsetObservable()
                               .Subscribe(offset =>
                                          {
                                              pen.DashOffset = offset;
                                              Invalidate();
                                          }));

            disposable.Add(info.CreateLineCapObservable()
                               .Subscribe(cap =>
                                          {
                                              pen.LineCap = cap;
                                              Invalidate();
                                          }));

            disposable.Add(info.CreateLineJoinObservable()
                               .Subscribe(join =>
                                          {
                                              pen.LineJoin = join;
                                              Invalidate();
                                          }));

            pen.Disposed += (s, e) => disposable.Dispose();

            return pen;
        }

        private void Invalidate()
        {
            if(!_suppressed) Context.Invalidate();
        }

        public void EnterReadLock() { _renderLock.EnterReadLock(); }

        public void EnterWriteLock() { _renderLock.EnterWriteLock(); }

        public void ExitReadLock() { _renderLock.ExitReadLock(); }

        public void ExitWriteLock() { _renderLock.ExitWriteLock(); }

        private TV Get<TK, TV>(IDictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
        {
            EnterReadLock();

            var exists = dict.TryGetValue(key, out var value) && value != null;
            var valid = (value as ResourceBase)?.IsDisposed != true;

            ExitReadLock();

            if (exists && valid)
                return value;

            EnterWriteLock();

            var newVal = dict[key] = fallback(key);

            ExitWriteLock();

            return newVal;
        }

        private IRenderImage LoadBitmap(IRenderContext target, string name)
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

        private void OnManagerAttached(object sender, EventArgs e)
        {
            if (sender is IViewManager vm) vm.RootUpdated += OnRootUpdated;
        }

        private void OnManagerDetached(object sender, EventArgs e)
        {
            if (sender is IViewManager vm)
            {
                vm.RootUpdated -= OnRootUpdated;
                ReleaseSceneResources();
            }
        }

        private void OnRootUpdated(object sender, PropertyChangedEventArgs e)
        {
            ReleaseSceneResources();

            if (Context.ViewManager?.Root != null)
                BindLayer(Context.ViewManager.Document.Root);
        }

        private void PeriodicUpdate(object state) { }

        private void Release<TKey, TValue>(IDictionary<TKey, TValue> dict) where TValue : IDisposable
        {
            foreach (var (_, value) in dict.AsTuples()) value?.Dispose();
            dict.Clear();
        }

        private void Release<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key) where TValue : IDisposable
        {
            if (!dict.TryGetValue(key, out var value)) return;

            value?.Dispose();
            dict.Remove(key);
        }

        #region ICacheManager Members

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            if (context.RenderContext != null)
            {
                LoadApplicationResources(context.RenderContext);

                if (context.ViewManager?.Root != null)
                    BindLayer(context.ViewManager.Document.Root);
            }

            Context = context;

            _timer = new Timer(PeriodicUpdate, null, 1000, 1000);

            context.ManagerDetached += OnManagerDetached;
            context.ManagerAttached += OnManagerAttached;
            context.RaiseAttached(this);
        }

        /// <inheritdoc />
        public void BindLayer(ILayer layer)
        {
            EnterWriteLock();

            if (layer is IFilledLayer filled)
                _fills[filled] = new SerialDisposerProperty<IBrush>(filled.CreateFillObservable().Select(BindBrush));

            if (layer is IStrokedLayer stroked)
                _strokes[stroked] = new SerialDisposerProperty<IPen>(stroked.CreateStrokeObservable().Select(BindStroke));

            if (layer is ITextLayer text)
                _texts[text] = new SerialDisposerProperty<ITextLayout>(text.CreateTextLayoutObservable(Context));

            if (layer is IImageLayer image)
                _images[image] = new SerialDisposerProperty<IRenderImage>(image.CreateImageObservable(Context));

            if (layer is IGeometricLayer geometric)
                _geometries[geometric] = new SerialDisposerProperty<IGeometry>(geometric.CreateGeometryObservable(Context));

            _bounds[layer] = new SerialProperty<RectangleF>(layer.CreateBoundsObservable(Context));

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
            context.ManagerAttached -= OnManagerAttached;
            context.RaiseDetached(this);
        }

        /// <inheritdoc />
        public RectangleF GetAbsoluteBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.AbsoluteTransform);
        }

        /// <inheritdoc />
        public IRenderImage GetBitmap(string key) { return _bitmaps[key]; }

        /// <inheritdoc />
        public RectangleF GetBounds(ILayer layer) { return _bounds.TryGet(layer)?.Value ?? layer.GetBounds(Context); }

        /// <inheritdoc />
        public IBrush GetBrush(string key) { return _brushes.TryGet(key); }

        /// <inheritdoc />
        public IBrush GetFill(IFilledLayer layer) { return _fills.TryGet(layer)?.Value ?? BindBrush(layer.Fill); }

        /// <inheritdoc />
        public IGeometry GetGeometry(IGeometricLayer layer)
        {
            return _geometries.TryGet(layer)?.Value ?? layer.GetGeometry(Context);
        }

        /// <inheritdoc />
        public IRenderImage GetImage(IImageLayer layer)
        {
            return _images.TryGet(layer)?.Value ?? layer.GetImage(Context);
        }

        /// <inheritdoc />
        public IPen GetPen(string key, int width)
        {
            return Get(_pens,
                       (key, width),
                       ((string k, int w) p) => Context.RenderContext.CreatePen(p.w, GetBrush(p.k)));
        }

        /// <inheritdoc />
        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public IPen GetStroke(IStrokedLayer layer) { return _strokes.TryGet(layer)?.Value ?? BindStroke(layer.Stroke); }

        public ITextLayout GetTextLayout(ITextLayer layer)
        {
            return _texts.TryGet(layer)?.Value ?? layer.GetLayout(Context);
        }

        /// <inheritdoc />
        public void LoadApplicationResources(IRenderContext target)
        {
            _bitmaps["cursor-resize-ns"] = LoadBitmap(target, "cursor-resize-ns");
            _bitmaps["cursor-resize-ew"] = LoadBitmap(target, "cursor-resize-ew");
            _bitmaps["cursor-resize-nwse"] = LoadBitmap(target, "cursor-resize-nwse");
            _bitmaps["cursor-resize-nesw"] = LoadBitmap(target, "cursor-resize-nesw");
            _bitmaps["cursor-rotate"] = LoadBitmap(target, "cursor-rotate");

            foreach (var key in AppSettings.Current.Theme.GetSubset("colors"))
            {
                if (!Color.TryParse(key.Value as string, out var color)) continue;

                var brush = target.CreateBrush(color);
                _brushes[key.Key] = brush;
                _pens[(key.Key, 1)] = target.CreatePen(1, brush);
            }
        }

        /// <inheritdoc />
        public void ReleaseDeviceResources()
        {
            EnterWriteLock();

            Release(_brushes);
            Release(_bitmaps);
            Release(_fills);
            Release(_strokes);
            Release(_images);

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void ReleaseResources()
        {
            EnterWriteLock();

            Release(_brushes);
            Release(_bitmaps);
            Release(_fills);
            Release(_strokes);
            Release(_images);
            Release(_geometries);
            Release(_texts);

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void ReleaseSceneResources()
        {
            EnterWriteLock();

            Release(_fills);
            Release(_strokes);
            Release(_geometries);
            Release(_texts);
            Release(_images);

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

            if (layer is IFilledLayer filled) Release(_fills, filled);
            if (layer is IStrokedLayer stroked) Release(_strokes, stroked);
            if (layer is IGeometricLayer geometric) Release(_geometries, geometric);
            if (layer is ITextLayer text) Release(_texts, text);

            _bounds.Remove(layer);

            ExitWriteLock();

            if (layer is IContainerLayer group)
                foreach (var subLayer in group.SubLayers)
                    UnbindLayer(subLayer);
        }

        public IArtContext Context { get; set; }

        #endregion

        #region Dictionaries

        private readonly Dictionary<string, IRenderImage> _bitmaps = new Dictionary<string, IRenderImage>();

        private readonly Dictionary<ILayer, ISerialProperty<RectangleF>> _bounds =
            new Dictionary<ILayer, ISerialProperty<RectangleF>>();

        private readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();

        private readonly Dictionary<IFilledLayer, ISerialProperty<IBrush>> _fills =
            new Dictionary<IFilledLayer, ISerialProperty<IBrush>>();

        private readonly Dictionary<IGeometricLayer, ISerialProperty<IGeometry>> _geometries =
            new Dictionary<IGeometricLayer, ISerialProperty<IGeometry>>();

        private readonly Dictionary<IImageLayer, ISerialProperty<IRenderImage>> _images =
            new Dictionary<IImageLayer, ISerialProperty<IRenderImage>>();

        private readonly Dictionary<(string, int), IPen> _pens = new Dictionary<(string, int), IPen>();

        private readonly Dictionary<IStrokedLayer, ISerialProperty<IPen>> _strokes =
            new Dictionary<IStrokedLayer, ISerialProperty<IPen>>();

        private readonly Dictionary<ITextLayer, ISerialProperty<ITextLayout>> _texts =
            new Dictionary<ITextLayer, ISerialProperty<ITextLayout>>();

        #endregion
    }
}