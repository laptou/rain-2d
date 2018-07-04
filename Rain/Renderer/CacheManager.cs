using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;
using Rain.Service;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;

namespace Rain.Renderer
{
    public class CacheManager : Core.Model.Model, ICacheManager
    {
        private readonly ReaderWriterLockSlim _renderLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool  _suppressed;
        private Timer _timer;

        public CacheManager(IArtContext context) { Context = context; }

        public IBrush BindBrush(IBrushInfo info)
        {
            if (info == null) return null;
            if (_brushes.ContainsKey(info)) return _brushes[info];

            var brush = info.CreateBrush(Context.RenderContext);
            var disposable = new CompositeDisposable();

            disposable.Add(info.CreateOpacityObservable()
                               .Subscribe(opacity =>
                                          {
                                              brush.Opacity = opacity;
                                              Invalidate();
                                          }));

            disposable.Add(info.CreateTransformObservable()
                               .Subscribe(transform =>
                                          {
                                              brush.Transform = transform;
                                              Invalidate();
                                          }));

            if (info is ISolidColorBrushInfo color &&
                brush is ISolidColorBrush colorBrush)
                disposable.Add(color.CreateColorObservable()
                                    .Subscribe(c =>
                                               {
                                                   colorBrush.Color = c;
                                                   Invalidate();
                                               }));

            if (info is IGradientBrushInfo gradient &&
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

            _brushes[info] = brush;

            brush.Disposed += (s, e) =>
                              {
                                  _brushes.Remove(info);
                                  disposable.Dispose();
                              };

            return brush;
        }

        public IPen BindStroke(IPenInfo info)
        {
            if (info == null) return default;
            if (_pens.ContainsKey(info)) return _pens[info];

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

            _pens[info] = pen;

            pen.Disposed += (s, e) =>
                            {
                                _pens.Remove(info);
                                disposable.Dispose();
                            };

            return pen;
        }

        private void Invalidate()
        {
            if (!_suppressed) Context.Invalidate();
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
            if (sender is IViewManager vm)
            {
                vm.RootUpdated += OnRootUpdated;

                if (vm.Root != null) BindLayer(vm.Root);
            }
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
            foreach (var value in dict.Values.ToArray())
                value?.Dispose();

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
                filled.CreateFillObservable().Subscribe(info => BindBrush(info));

            if (layer is IStrokedLayer stroked)
                stroked.CreateStrokeObservable().Subscribe(info => BindStroke(info));

            if (layer is ITextLayer text)
                _texts[text] = new SerialDisposerProperty<ITextLayout>(text.CreateTextLayoutObservable(Context));

            if (layer is IImageLayer image)
                _images[image] = new SerialDisposerProperty<IRenderImage>(image.CreateImageObservable(Context));

            if (layer is IGeometricLayer geometric)
            {
                var window = TimeSpan.FromSeconds(5);

                var geometry = geometric.CreateGeometryObservable(Context)
                                        .Throttle(window)
                                        .Where(g => g != null && !g.IsDisposed);


                var optimizedFill = geometry.Select(g => g.Optimize());

                var pen = geometric.CreateStrokeObservable();

                //var optimizedStroke = geometry.CombineLatest(pen.Throttle(window), (g, p) => g.Optimize(p))
                //                              .Merge(optimizedFill.Where(
                //                                         f => f.OptimizationMode.HasFlag(
                //                                             GeometryOptimizationMode.Stroke)));

                _fillGeometries[geometric] = new SerialDisposerProperty<IGeometry>(optimizedFill.Merge(geometry));
                _strokeGeometries[geometric] = new SerialDisposerProperty<IGeometry>(geometry);
            }

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
        public IBrush GetBrush(IBrushInfo brush) { return _brushes.TryGet(brush) ?? BindBrush(brush); }

        /// <inheritdoc />
        public IGeometry GetFillGeometry(IGeometricLayer layer)
        {
            return _fillGeometries.TryGet(layer)?.Value ?? layer.GetGeometry(Context);
        }

        /// <inheritdoc />
        public IGeometry GetStrokeGeometry(IGeometricLayer layer)
        {
            return _strokeGeometries.TryGet(layer)?.Value ?? layer.GetGeometry(Context);
        }

        /// <inheritdoc />
        public IRenderImage GetImage(IImageLayer layer)
        {
            return _images.TryGet(layer)?.Value ?? layer.GetImage(Context);
        }

        /// <inheritdoc />
        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public IPen GetPen(IPenInfo pen) { return _pens.TryGet(pen) ?? BindStroke(pen); }

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

                // TODO: Fix this
                //_brushes[key.Key] = brush;
                //_pens[(key.Key, 1)] = target.CreatePen(1, brush);
            }
        }

        /// <inheritdoc />
        public void ReleaseDeviceResources()
        {
            EnterWriteLock();

            Release(_brushes);
            Release(_bitmaps);
            Release(_brushes);
            Release(_pens);
            Release(_images);

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void ReleaseResources()
        {
            EnterWriteLock();

            Release(_brushes);
            Release(_bitmaps);
            Release(_brushes);
            Release(_pens);
            Release(_images);
            Release(_fillGeometries);
            Release(_strokeGeometries);
            Release(_texts);

            ExitWriteLock();
        }

        /// <inheritdoc />
        public void ReleaseSceneResources()
        {
            EnterWriteLock();

            Release(_brushes);
            Release(_pens);
            Release(_fillGeometries);
            Release(_strokeGeometries);
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

            if (layer is IGeometricLayer geometric)
            {
                Release(_fillGeometries, geometric);
                Release(_strokeGeometries, geometric);
            }

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

        private readonly Dictionary<IBrushInfo, IBrush> _brushes = new Dictionary<IBrushInfo, IBrush>();

        private readonly Dictionary<IGeometricLayer, ISerialProperty<IGeometry>> _fillGeometries =
            new Dictionary<IGeometricLayer, ISerialProperty<IGeometry>>();

        private readonly Dictionary<IGeometricLayer, ISerialProperty<IGeometry>> _strokeGeometries =
            new Dictionary<IGeometricLayer, ISerialProperty<IGeometry>>();

        private readonly Dictionary<IImageLayer, ISerialProperty<IRenderImage>> _images =
            new Dictionary<IImageLayer, ISerialProperty<IRenderImage>>();

        private readonly Dictionary<IPenInfo, IPen> _pens = new Dictionary<IPenInfo, IPen>();

        private readonly Dictionary<ITextLayer, ISerialProperty<ITextLayout>> _texts =
            new Dictionary<ITextLayer, ISerialProperty<ITextLayout>>();

        #endregion
    }
}