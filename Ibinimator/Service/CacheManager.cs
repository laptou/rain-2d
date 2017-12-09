using System.Collections.Generic;
using System;
using System.ComponentModel;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;

namespace Ibinimator.Service
{
    public class CacheManager : Model, ICacheManager
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

        private readonly object _renderLock = new object();

        private readonly Dictionary<(ILayer layer, int id), IDisposable> _resources =
            new Dictionary<(ILayer layer, int id), IDisposable>();

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
                _brushBindings[brush] = (shape, fill);

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

        public void UnbindLayer(ILayer layer)
        {
            if (layer is IFilledLayer filled)
            {
                if (filled.Fill != null)
                    lock (_fills)
                    {
                        if (_fills.TryGetValue(filled, out var fill))
                        {
                            fill?.Dispose();
                            _fills.Remove(filled);
                        }
                    }
            }

            if (layer is IStrokedLayer stroked)
            {
                lock (_strokes)
                {
                    if (_strokes.TryGetValue(stroked, out var stroke))
                    {
                        stroke.Dispose();
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

        private TV Get<TK, TV>(Dictionary<TK, TV> dict, TK key, Func<TK, TV> fallback)
        {
            lock (dict)
            {
                if (dict.TryGetValue(key, out var value) &&
                    (value as ResourceBase)?.Disposed != true &&
                    value != null)
                    return value;

                return dict[key] = fallback(key);
            }
        }

        private IBitmap LoadBitmap(RenderContext target, string name)
        {
            var uri = new Uri($"./Resources/Icon/{name}.png", UriKind.Relative);


            if (target.GetDpi() > 96)
                uri = new Uri($"./Resources/Icon/{name}@2x.png", UriKind.Relative);

            using (var stream = Application.GetResourceStream(uri)?.Stream)
                return target.CreateBitmap(stream);
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
            }

            Context.InvalidateSurface();
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
                    stroke.Dashes.Clear();
                    // TODO: FIX dashes and stuff
                    break;
            }

            Context.InvalidateSurface();
        }

        #region ICacheManager Members

        public void BindLayer(ILayer layer)
        {
            if (layer is IFilledLayer filled)
            {
                lock (_fills)
                {
                    _fills.TryGet(filled)?.Dispose();
                    _fills[filled] = BindBrush(filled, filled.Fill);
                }

                filled.FillChanged += (s, e) =>
                {
                    lock (_renderLock)
                        if (s is IFilledLayer shape)
                            _fills.TryGet(shape)?.Dispose();

                    Context.InvalidateSurface();
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
                    lock (_renderLock)
                        if (s is IStrokedLayer shape)
                            _strokes.TryGet(shape)?.Dispose();

                    Context.InvalidateSurface();
                };
            }

            if (layer is ITextLayer text)
            {
                text.LayoutChanged += (s, e) =>
                {
                    lock (_renderLock)
                        if (s is ITextLayer t)
                            _texts.TryGet(t)?.Dispose();

                    Context.InvalidateSurface();
                };
            }

            if (layer is IGeometricLayer geometric)
            {
                geometric.GeometryChanged += (s, e) =>
                {
                    lock (_renderLock)
                        if (s is IGeometricLayer g)
                            _geometries.TryGet(g)?.Dispose();

                    Context.InvalidateSurface();
                };
            }

            layer.BoundsChanged += (s, e) =>
            {
                if (s is ILayer l)
                {
                    lock (_bounds)
                        _bounds[l] = l.GetBounds(this);
                    RaiseBoundsChanged(l);
                }

                Context.InvalidateSurface();
            };

            if (layer is IContainerLayer group)
            {
                foreach (var subLayer in group.SubLayers)
                    BindLayer(subLayer);

                group.LayerAdded +=
                    (sender, layer1) =>
                        BindLayer(layer1);
            }
        }

        private void RaiseBoundsChanged(ILayer layer) { BoundsChanged?.Invoke(this, layer); }

        public event EventHandler<ILayer> BoundsChanged;

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

        public IBitmap GetBitmap(string key) { return _bitmaps[key]; }

        public RectangleF GetBounds(ILayer layer) { return Get(_bounds, layer, l => l.GetBounds(this)); }

        public IBrush GetBrush(string key) { return _brushes[key]; }

        public IBrush GetFill(IFilledLayer layer) { return Get(_fills, layer, l => BindBrush(l, l.Fill)); }

        public IGeometry GetGeometry(IGeometricLayer layer)
        {
            return Get(_geometries, layer, l => l.GetGeometry(this));
        }

        public RectangleF GetRelativeBounds(ILayer layer)
        {
            return MathUtils.Bounds(GetBounds(layer), layer.Transform);
        }

        public IEnumerable<(int id, T resource)> GetResources<T>(ILayer layer)
            where T : IDisposable
        {
            lock (_resources)
            {
                return _resources.Where(kv => kv.Key.layer == layer)
                                 .Select(kv => (kv.Key.id, (T) kv.Value))
                                 .ToArray();
            }
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
            _brushes[nameof(EditorColors.ArtSpace)] = target.CreateBrush(EditorColors.ArtSpace);
            _brushes[nameof(EditorColors.Artboard)] = target.CreateBrush(EditorColors.Artboard);

            _brushes[nameof(EditorColors.SelectionHandle)] =
                target.CreateBrush(EditorColors.SelectionHandle);
            _brushes[nameof(EditorColors.SelectionHandleOutline)] =
                target.CreateBrush(EditorColors.SelectionHandleOutline);
            _brushes[nameof(EditorColors.SelectionOutline)] =
                target.CreateBrush(EditorColors.SelectionOutline);

            _brushes[nameof(EditorColors.Node)] = target.CreateBrush(EditorColors.Node);
            _brushes[nameof(EditorColors.NodeClick)] = target.CreateBrush(EditorColors.NodeClick);
            _brushes[nameof(EditorColors.NodeHover)] = target.CreateBrush(EditorColors.NodeHover);
            _brushes[nameof(EditorColors.NodeOutline)] = target.CreateBrush(EditorColors.NodeOutline);
            _brushes[nameof(EditorColors.NodeOutlineAlt)] = target.CreateBrush(EditorColors.NodeOutlineAlt);
            _brushes[nameof(EditorColors.NodeSelected)] = target.CreateBrush(EditorColors.NodeSelected);

            _brushes[nameof(EditorColors.TextCaret)] = target.CreateBrush(EditorColors.TextCaret);
            _brushes[nameof(EditorColors.TextHighlight)] = target.CreateBrush(EditorColors.TextHighlight);

            _brushes[nameof(EditorColors.Guide)] = target.CreateBrush(EditorColors.Guide);
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
                foreach (var (IBrushInfo, (_, brush)) in _brushBindings.AsTuples())
                {
                    if (IBrushInfo != null)
                        IBrushInfo.PropertyChanged -= OnBrushPropertyChanged;
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

            lock (_fills)
            {
                foreach (var (_, fill) in _fills.AsTuples()) fill?.Dispose();
                _fills.Clear();
            }

            lock (_strokes)
            {
                foreach (var (_, stroke) in _strokes.AsTuples()) stroke?.Dispose();
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

        public IArtContext Context { get; set; }

        #endregion
    }
}