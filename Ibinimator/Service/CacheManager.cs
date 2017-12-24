using System.Collections.Generic;
using System;
using System.ComponentModel;

using Ibinimator.Core.Utility;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;

namespace Ibinimator.Service
{
    public class CacheManager : Core.Model.Model, ICacheManager
    {
        private readonly Dictionary<string, IBitmap> _bitmaps =
            new Dictionary<string, IBitmap>();

        private readonly Dictionary<ILayer, RectangleF> _bounds =
            new Dictionary<ILayer, RectangleF>();

        private readonly Dictionary<IBrushInfo, (ILayer layer, IBrush brush)>
            _brushBindings =
                new Dictionary<IBrushInfo, (ILayer, IBrush)>();

        private readonly Dictionary<string, IBrush> _brushes =
            new Dictionary<string, IBrush>();

        private readonly Dictionary<IFilledLayer, IBrush> _fills =
            new Dictionary<IFilledLayer, IBrush>();

        private readonly Dictionary<IGeometricLayer, IGeometry> _geometries =
            new Dictionary<IGeometricLayer, IGeometry>();

        private readonly ReaderWriterLockSlim _renderLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly Dictionary<IPenInfo, (IStrokedLayer layer, IPen stroke)>
            _strokeBindings =
                new Dictionary<IPenInfo, (IStrokedLayer layer, IPen stroke)>();

        private readonly Dictionary<IStrokedLayer, IPen> _strokes =
            new Dictionary<IStrokedLayer, IPen>();

        private readonly Dictionary<ITextLayer, ITextLayout> _texts =
            new Dictionary<ITextLayer, ITextLayout>();

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

        private IBitmap LoadBitmap(RenderContext target, string name)
        {
            var uri = new Uri($"./Resources/Icon/{name}.png", UriKind.Relative);


            if (target.GetDpi() > 96)
                uri = new Uri($"./Resources/Icon/{name}@2x.png", UriKind.Relative);

            using (var stream = Application.GetResourceStream(uri)?.Stream)
            {
                return target.CreateBitmap(stream);
            }
        }

        private void OnBrushPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var brush = (IBrushInfo) sender;
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
                    ((ISolidColorBrush) fill).Color = ((SolidColorBrushInfo) brush).Color;

                    break;

                case "Stops":
                    ((IGradientBrush) fill).Stops.ReplaceRange(((GradientBrushInfo) brush).Stops);

                    break;
            }

            Context.InvalidateRender();
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

            Context.InvalidateRender();
        }

        #region ICacheManager Members

        /// <inheritdoc />
        public void Attach(IArtContext context) { }


        /// <inheritdoc />
        public void BindLayer(ILayer layer)
        {
            EnterWriteLock();

            if (layer is IFilledLayer filled)
            {
                lock (_fills)
                {
                    _fills.TryGet(filled)?.Dispose();
                    _fills[filled] = BindBrush(filled, filled.Fill);
                }

                filled.FillChanged += (s, e) =>
                                      {
                                          EnterWriteLock();

                                          if (s is IFilledLayer shape)
                                              _fills.TryGet(shape)?.Dispose();

                                          ExitWriteLock();

                                          Context.InvalidateRender();
                                      };
            }

            if (layer is IStrokedLayer stroked)
            {
                lock (_strokes)
                {
                    _strokes.TryGet(stroked)?.Dispose();
                    _strokes[stroked] = BindStroke(stroked, stroked.Stroke);
                }

                stroked.StrokeChanged += (s, e) =>
                                         {
                                             EnterWriteLock();

                                             if (s is IStrokedLayer shape)
                                                 _strokes.TryGet(shape)?.Dispose();

                                             ExitWriteLock();

                                             Context.InvalidateRender();
                                         };
            }

            if (layer is ITextLayer text)
                text.LayoutChanged += (s, e) =>
                                      {
                                          EnterWriteLock();

                                          if (s is ITextLayer t)
                                              _texts.TryGet(t)?.Dispose();

                                          ExitWriteLock();

                                          Context.InvalidateRender();
                                      };

            if (layer is IGeometricLayer geometric)
                geometric.GeometryChanged += (s, e) =>
                                             {
                                                 EnterWriteLock();

                                                 if (s is IGeometricLayer g)
                                                     _geometries.TryGet(g)?.Dispose();

                                                 ExitWriteLock();

                                                 Context.InvalidateRender();
                                             };

            layer.BoundsChanged += (s, e) =>
                                   {
                                       if (s is ILayer l)
                                       {
                                           EnterWriteLock();
                                           _bounds[l] = l.GetBounds(this);
                                           ExitWriteLock();
                                       }

                                       Context.InvalidateRender();
                                   };

            ExitWriteLock();

            if (layer is IContainerLayer group)
            {
                foreach (var subLayer in group.SubLayers)
                    BindLayer(subLayer);

                group.LayerAdded +=
                    (sender, layer1) =>
                        BindLayer(layer1);
            }
        }

        /// <inheritdoc />
        public void Detach(IArtContext context) { }

        /// <inheritdoc />
        public RectangleF GetAbsoluteBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.AbsoluteTransform);
        }

        public IBitmap GetBitmap(string key) { return _bitmaps[key]; }

        /// <inheritdoc />
        public RectangleF GetBounds(ILayer layer) { return Get(_bounds, layer, l => l.GetBounds(this)); }

        public IBrush GetBrush(string key) { return _brushes[key]; }

        public IBrush GetFill(IFilledLayer layer) { return Get(_fills, layer, l => BindBrush(l, l.Fill)); }

        public IGeometry GetGeometry(IGeometricLayer layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(this));
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

        public ITextLayout GetTextLayout(ITextLayer text)
        {
            return Get(_texts, text, t => t.GetLayout(Context));
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

        public void ResetDeviceResources()
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

            ExitWriteLock();
        }

        public void ResetResources()
        {
            ResetDeviceResources();

            EnterWriteLock();

            foreach (var (_, geometry) in _geometries.AsTuples()) geometry?.Dispose();
            _geometries.Clear();

            foreach (var (_, layout) in _texts.AsTuples())
                layout.Dispose();

            _texts.Clear();

            ExitWriteLock();
        }

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