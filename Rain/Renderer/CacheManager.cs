using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            var subscriptions = new LinkedList<IDisposable>();

            subscriptions.AddLast(fill.CreateOpacityObservable()
                                      .Subscribe(opacity =>
                                                 {
                                                     brush.Opacity = opacity;
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(fill.CreateTransformObservable()
                                      .Subscribe(transform =>
                                                 {
                                                     brush.Transform = transform;
                                                     Context.Invalidate();
                                                 }));

            if (fill is ISolidColorBrushInfo color &&
                brush is ISolidColorBrush colorBrush)
                subscriptions.AddLast(color.CreateColorObservable()
                                           .Subscribe(c =>
                                                      {
                                                          colorBrush.Color = c;
                                                          Context.Invalidate();
                                                      }));

            if (fill is IGradientBrushInfo gradient &&
                brush is IGradientBrush gradientBrush)
            {
                subscriptions.AddLast(gradient.CreateStopsObservable()
                                              .Subscribe(stops =>
                                                         {
                                                             gradientBrush.Stops.ReplaceRange(stops);
                                                             Context.Invalidate();
                                                         }));

                if (brush is ILinearGradientBrush linear)
                {
                    subscriptions.AddLast(gradient.CreateStartPointObservable()
                                                  .Subscribe(start =>
                                                             {
                                                                 (linear.StartX, linear.StartY) = start;
                                                                 Context.Invalidate();
                                                             }));
                    subscriptions.AddLast(gradient.CreateEndPointObservable()
                                                  .Subscribe(end =>
                                                             {
                                                                 (linear.EndX, linear.EndY) = end;
                                                                 Context.Invalidate();
                                                             }));
                }

                if (brush is IRadialGradientBrush radial)
                {
                    subscriptions.AddLast(gradient.CreateStartPointObservable()
                                                  .Subscribe(start =>
                                                             {
                                                                 (radial.CenterX, radial.CenterY) = start;
                                                                 Context.Invalidate();
                                                             }));

                    subscriptions.AddLast(gradient.CreateEndPointObservable()
                                                  .Subscribe(end =>
                                                             {
                                                                 (radial.RadiusX, radial.RadiusY) =
                                                                     end - gradient.StartPoint;
                                                                 Context.Invalidate();
                                                             }));
                }
            }

            brush.Disposed += (s, e) =>
                              {
                                  foreach (var subscription in subscriptions) subscription.Dispose();
                              };

            return brush;
        }

        public IPen BindStroke(IPenInfo info)
        {
            if (info == null) return default;

            var pen = info.CreatePen(Context.RenderContext);
            var subscriptions = new LinkedList<IDisposable>();

            subscriptions.AddLast(info.CreateBrushObservable()
                                      .Subscribe(brush =>
                                                 {
                                                     pen.Brush = BindBrush(brush);
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(info.CreateWidthObservable()
                                      .Subscribe(width =>
                                                 {
                                                     pen.Width = width;
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(info.CreateDashesObservable()
                                      .Subscribe(dashes =>
                                                 {
                                                     pen.Dashes.ReplaceAll(dashes);
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(info.CreateDashOffsetObservable()
                                      .Subscribe(offset =>
                                                 {
                                                     pen.DashOffset = offset;
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(info.CreateLineCapObservable()
                                      .Subscribe(cap =>
                                                 {
                                                     pen.LineCap = cap;
                                                     Context.Invalidate();
                                                 }));

            subscriptions.AddLast(info.CreateLineJoinObservable()
                                      .Subscribe(join =>
                                                 {
                                                     pen.LineJoin = join;
                                                     Context.Invalidate();
                                                 }));

            pen.Disposed += (s, e) =>
                            {
                                foreach (var subscription in subscriptions) subscription.Dispose();
                            };

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
                _fills[filled] = filled.CreateFillObservable().Select(BindBrush).Disposer();

            if (layer is IStrokedLayer stroked)
                _strokes[stroked] = stroked.CreateStrokeObservable().Select(BindStroke).Disposer();

            if (layer is ITextLayer text)
                _texts[text] = text.CreateTextLayoutObservable(Context).Disposer();

            if (layer is IImageLayer image)
                _images[image] = image.CreateImageObservable(Context).Disposer();

            if (layer is IGeometricLayer geometric)
                _geometries[geometric] = geometric.CreateGeometryObservable(Context).Disposer();

            _bounds[layer] = new ObservableProperty<RectangleF>(layer.CreateBoundsObservable(Context));

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
        public RectangleF GetBounds(ILayer layer) { return _bounds[layer].Value; }

        /// <inheritdoc />
        public IBrush GetBrush(string key) { return _brushes.TryGet(key); }

        /// <inheritdoc />
        public IBrush GetFill(IFilledLayer layer) { return _fills[layer].Value; }

        /// <inheritdoc />
        public IGeometry GetGeometry(IGeometricLayer layer) { return _geometries[layer].Value; }

        /// <inheritdoc />
        public IRenderImage GetImage(IImageLayer layer) { return _images[layer].Value; }

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

        public IPen GetStroke(IStrokedLayer layer) { return _strokes[layer].Value; }

        public ITextLayout GetTextLayout(ITextLayer layer) { return _texts[layer].Value; }

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

        private readonly Dictionary<ILayer, IObservableProperty<RectangleF>> _bounds =
            new Dictionary<ILayer, IObservableProperty<RectangleF>>();

        private readonly Dictionary<string, IBrush> _brushes = new Dictionary<string, IBrush>();

        private readonly Dictionary<IFilledLayer, IObservableProperty<IBrush>> _fills =
            new Dictionary<IFilledLayer, IObservableProperty<IBrush>>();

        private readonly Dictionary<IGeometricLayer, IObservableProperty<IGeometry>> _geometries =
            new Dictionary<IGeometricLayer, IObservableProperty<IGeometry>>();

        private readonly Dictionary<IImageLayer, IObservableProperty<IRenderImage>> _images =
            new Dictionary<IImageLayer, IObservableProperty<IRenderImage>>();

        private readonly Dictionary<(string, int), IPen> _pens = new Dictionary<(string, int), IPen>();

        private readonly Dictionary<IStrokedLayer, IObservableProperty<IPen>> _strokes =
            new Dictionary<IStrokedLayer, IObservableProperty<IPen>>();

        private readonly Dictionary<ITextLayer, IObservableProperty<ITextLayout>> _texts =
            new Dictionary<ITextLayer, IObservableProperty<ITextLayout>>();

        #endregion
    }
}