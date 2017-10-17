using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Windows.Input;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Renderer.Model.Layer;

namespace Ibinimator.Service
{
    public enum SelectionResizeHandle
    {
        Rotation = -2,
        Translation = -1,
        Top = 1,
        Left = 2,
        Right = 4,
        Bottom = 8,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right
    }


    public class SelectionManager : Model, ISelectionManager
    {
        private readonly object _render = new object();
        private Vector2 _accumulatedTranslation;
        private Vector2? _beginPosition;
        private Vector2? _lastPosition;
        private bool _moved;
        private bool _selecting;
        private RectangleF _selectionBox;
        private StrokeStyle1 _selectionStroke;
        private SelectionResizeHandle? _transformHandle;

        public SelectionManager(ArtView artView, IViewManager viewManager, IHistoryManager historyManager)
        {
            Context = artView;
            artView.RenderTargetBound += OnRenderTargetBound;

            Selection = new ObservableList<Layer>();
            Selection.CollectionChanged += (sender, args) =>
            {
                Update(true);
                Updated?.Invoke(this, null);
            };

            viewManager.DocumentUpdated += (sender, args) =>
            {
                var layer = (Layer) sender;
                switch (args.PropertyName)
                {
                    case "Bounds":
                        if (layer.Selected)
                            Update(true);
                        break;
                    case nameof(Layer.Selected):
                        var contains = Selection.Contains(layer);

                        if (layer.Selected && !contains)
                            Selection.Add(layer);
                        else if (!layer.Selected && contains)
                            Selection.Remove(layer);
                        break;
                }
            };

            historyManager.Traversed += (sender, args) => { Update(true); };
        }

        public ObservableList<Layer> Selection { get; }

        private (IBitmap cursor, SelectionResizeHandle? handle) HandleTest(Vector2 pos)
        {
            List<(Vector2 pos, string cur, SelectionResizeHandle handle)> handles =
                new List<(Vector2, string, SelectionResizeHandle)>();

            pos = FromSelectionSpace(pos);

            Vector2 tl = SelectionBounds.TopLeft,
                br = SelectionBounds.BottomRight;

            float x1 = tl.X,
                y1 = tl.Y,
                x2 = br.X,
                y2 = br.Y;

            handles.Add((new Vector2(x1, y1), "nwse", SelectionResizeHandle.TopLeft));
            handles.Add((new Vector2(x2, y1), "nesw", SelectionResizeHandle.TopRight));
            handles.Add((new Vector2(x2, y2), "nwse", SelectionResizeHandle.BottomRight));
            handles.Add((new Vector2(x1, y2), "nesw", SelectionResizeHandle.BottomLeft));
            handles.Add((new Vector2((x1 + x2) / 2, y1), "ns", SelectionResizeHandle.Top));
            handles.Add((new Vector2(x1, (y1 + y2) / 2), "ew", SelectionResizeHandle.Left));
            handles.Add((new Vector2(x2, (y1 + y2) / 2), "ew", SelectionResizeHandle.Right));
            handles.Add((new Vector2((x1 + x2) / 2, y2), "ns", SelectionResizeHandle.Bottom));
            handles.Add(
                (new Vector2((x1 + x2) / 2, y1 - 10 / Context.ViewManager.Zoom), "rot", SelectionResizeHandle
                    .Rotation));

            foreach (var h in handles)
                if ((pos - h.pos).LengthSquared() < 49 / Context.ViewManager.Zoom)
                    return (Context.CacheManager.GetBitmap("cursor-" + h.cur), h.handle);

            return (null, null);
        }

        private void InvalidateSurface()
        {
            Context.InvalidateSurface();
        }

        private void OnRenderTargetBound(object sender, RenderTarget target)
        {
            _selectionStroke = new StrokeStyle1(target.Factory.QueryInterface<Factory1>(),
                new StrokeStyleProperties1 {TransformType = StrokeTransformType.Hairline});
        }

        private void Resize(Vector2 pos, bool uniform)
        {
            var scale = Vector2.One;
            var translate = Vector2.Zero;
            float rotate = 0;

            var origin = Vector2.Zero;
            var axis = Vector2.Zero;
            var rpos = FromSelectionSpace(pos);

            switch (_transformHandle)
            {
                case SelectionResizeHandle.Top:
                    origin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Bottom);
                    axis = new Vector2(0, 1);
                    break;

                case SelectionResizeHandle.Bottom:
                    origin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Top);
                    axis = new Vector2(0, -1);
                    break;

                case SelectionResizeHandle.Left:
                    origin = new Vector2(SelectionBounds.Right, SelectionBounds.Center.Y);
                    axis = new Vector2(1, 0);
                    break;

                case SelectionResizeHandle.Right:
                    origin = new Vector2(SelectionBounds.Left, SelectionBounds.Center.Y);
                    axis = new Vector2(-1, 0);
                    break;

                case SelectionResizeHandle.TopRight:
                    origin = SelectionBounds.BottomLeft;
                    axis = new Vector2(-1, 1);
                    break;

                case SelectionResizeHandle.TopLeft:
                    origin = SelectionBounds.BottomRight;
                    axis = new Vector2(1, 1);
                    break;

                case SelectionResizeHandle.BottomRight:
                    origin = SelectionBounds.TopLeft;
                    axis = new Vector2(-1, -1);
                    break;

                case SelectionResizeHandle.BottomLeft:
                    origin = SelectionBounds.TopRight;
                    axis = new Vector2(1, -1);
                    break;

                case SelectionResizeHandle.Translation:
                    if (_lastPosition != null)
                        translate = pos - _lastPosition.Value;
                    break;

                case SelectionResizeHandle.Rotation:
                    origin = SelectionBounds.Center;
                    var x = rpos - origin;
                    var angle = (float) Math.Atan2(x.Y, x.X) + MathUtils.PiOverTwo;
                    rotate = angle - SelectionShear;
                    // Trace.TraceInformation($"rotation: {rotate:F2}");
                    break;
            }

            if (axis != Vector2.Zero)
            {
                axis.Y *= Math.Abs(SelectionBounds.Height / SelectionBounds.Width);

                var crossSection = MathUtils.CrossSection(axis, origin, SelectionBounds);
                var axisLength = MathUtils.AbsMax(0.001f, (crossSection.Item2 - crossSection.Item1).Length());

                //origin = ToSelectionSpace(origin);
                //axis = Vector2.Normalize(MathUtils.Rotate(MathUtils.ShearX(axis, selectionShear), selectionRotation));
                axis = Vector2.Normalize(axis);

                if (uniform)
                    scale =
                        MathUtils.Project(rpos - origin, axis) *
                        -MathUtils.Sign(axis) / axisLength +
                        Vector2.One - MathUtils.Abs(axis);
                else
                    scale =
                        (rpos - origin) *
                        -MathUtils.Sign(axis) / axisLength +
                        Vector2.One - MathUtils.Abs(axis);

                if (float.IsNaN(scale.X) || float.IsNaN(scale.Y)) Debugger.Break();

                // don't let them scale to 0, otherwise we can't scale back
                // because 0 x 0 = 0
                if (scale.X < 1 / SelectionBounds.Width)
                    scale.X = -1 / SelectionBounds.Width;

                if (scale.Y < 1 / SelectionBounds.Height)
                    scale.Y = -1 / SelectionBounds.Height;
            }

            if (translate != Vector2.Zero && uniform)
            {
                var delta = (_lastPosition - _beginPosition).GetValueOrDefault();

                var newDelta = _accumulatedTranslation + translate;

                var angle = Math.Abs(delta.Y / delta.X);

                if (angle < MathUtils.InverseSqrt3) // tan (30 degrees)
                    newDelta = new Vector2(newDelta.Length() * Math.Sign(newDelta.X), 0);
                else if (angle < MathUtils.Sqrt3) // tan(60 degree  s)
                    newDelta =
                        newDelta.Length() *
                        Math.Sign(newDelta.Y) *
                        new Vector2(
                            MathUtils.InverseSqrt2,
                            MathUtils.InverseSqrt2 * Math.Sign(newDelta.Y / newDelta.X));
                else
                    newDelta = new Vector2(0, newDelta.Length() * Math.Sign(newDelta.Y));

                translate = newDelta - _accumulatedTranslation;
            }

            if (Math.Abs(rotate) > MathUtils.PiOverFour && uniform)
                rotate = Math.Sign(rotate) * MathUtils.PiOverFour;

            if (scale.X < 0)
                _transformHandle = _transformHandle ^ SelectionResizeHandle.Left ^ SelectionResizeHandle.Right;

            if (scale.Y < 0)
                _transformHandle = _transformHandle ^ SelectionResizeHandle.Top ^ SelectionResizeHandle.Bottom;

            var relativeOrigin = (origin - SelectionBounds.TopLeft)
                                 / MathUtils.Abs(new Vector2(SelectionBounds.Width, SelectionBounds.Height));

            _accumulatedTranslation += translate;


            Transform(
                scale,
                translate,
                rotate,
                0,
                relativeOrigin,
                true);
        }

        private void Select(Vector2 pos)
        {
            InvalidateSurface();

            if (pos.X < _selectionBox.Left)
                _selectionBox.Left = pos.X;
            else
                _selectionBox.Right = pos.X;

            if (pos.Y < _selectionBox.Top)
                _selectionBox.Top = pos.Y;
            else
                _selectionBox.Bottom = pos.Y;

            InvalidateSurface();
        }

        private void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin,
            bool continuous)
        {
            var wlock = new WeakLock(_render);

            var size = new Vector2(
                Math.Abs(SelectionBounds.Width),
                Math.Abs(SelectionBounds.Height));
            origin *= size;
            origin += SelectionBounds.TopLeft;
            var so = ToSelectionSpace(origin);

            var transform =
                Matrix3x2.CreateRotation(-SelectionRotation, SelectionBounds.Center) *
                Matrix3x2.CreateTranslation(-SelectionBounds.Center) *
                Matrix3x2.CreateSkew(0, -SelectionShear) *
                Matrix3x2.CreateScale(scale.X, scale.Y, origin - SelectionBounds.Center) *
                Matrix3x2.CreateSkew(0, SelectionShear + shear) *
                Matrix3x2.CreateTranslation(SelectionBounds.Center) *
                Matrix3x2.CreateRotation(SelectionRotation, SelectionBounds.Center) *
                Matrix3x2.CreateRotation(rotate, so) *
                Matrix3x2.CreateTranslation(translate);

            var history = Context.HistoryManager;

            if (continuous && history.Current is TransformCommand lastTransformCommand)
            {
                var current =
                    new TransformCommand(
                        0,
                        Selection.ToArray<ILayer>(),
                        transform);

                current.Do(Context);

                history.Replace(
                    new TransformCommand(
                        Context.HistoryManager.Position,
                        Selection.ToArray<ILayer>(),
                        lastTransformCommand.Transform * transform));
            }
            else
            {
                var current = new TransformCommand(
                    Context.HistoryManager.Position + 1,
                    Selection.ToArray<ILayer>(), transform);

                history.Do(current);
            }

            var sb = SelectionBounds;
            var tl = MathUtils.Scale(sb.TopLeft, origin, scale);
            var br = MathUtils.Scale(sb.BottomRight, origin, scale);
            (tl, br) = (Vector2.Min(tl, br), Vector2.Max(tl, br));

            sb = new RectangleF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);

            var newCenter = Vector2.Transform(SelectionBounds.Center, transform);
            var sdelta = newCenter - sb.Center;
            sb.Offset(sdelta);

            SelectionBounds = sb;
            SelectionRotation += rotate;
            SelectionShear += shear;

            Updated?.Invoke(this, null);
            InvalidateSurface();

            wlock.Dispose();
        }

        private void UpdateCursor(Vector2 pos)
        {
            if (_transformHandle == null)
                Cursor = Selection.Count > 0 ? HandleTest(pos).cursor : null;

            InvalidateSurface();
        }

        #region ISelectionManager Members

        public event EventHandler Updated;

        public void ClearSelection()
        {
            while (Selection.Count > 0)
                Selection[0].Selected = false;
        }

        public Vector2 FromSelectionSpace(Vector2 v)
        {
            return MathUtils.ShearX(
                MathUtils.Rotate(v, SelectionBounds.Center, -SelectionRotation),
                SelectionBounds.Center,
                -SelectionShear);
        }

        public void MouseDown(Vector2 pos)
        {
            using (new WeakLock(this))
            {
                _moved = false;

                if (Selection.Count > 0 && Context.ToolManager.Type == ToolType.Select)
                {
                    _transformHandle = HandleTest(pos).handle;

                    if (_transformHandle == null &&
                        Selection.Any(l => l.Hit(Context.CacheManager, pos, true) != null))
                        _transformHandle = SelectionResizeHandle.Translation;
                }

                if (Selection.Count == 0 && !_selecting)
                {
                    _selectionBox = new RectangleF(pos.X, pos.Y, 0, 0);
                    _selecting = true;
                }

                _lastPosition = pos;
                _beginPosition = pos;
            }
        }

        public void MouseMove(Vector2 pos)
        {
            var modifiers = App.Dispatcher.Invoke(() => Keyboard.Modifiers);

            using (new WeakLock(this))
            {
                if (!_moved && _transformHandle != null)
                    Context.HistoryManager.Do(
                        new TransformCommand(
                            Context.HistoryManager.Position + 1,
                            Selection.ToArray<ILayer>(),
                            Matrix3x2.Identity));

                _moved = true;

                _lastPosition = _lastPosition ?? pos;

                float width = Math.Max(1f, SelectionBounds.Width),
                    height = Math.Max(1f, SelectionBounds.Height);

                if (_transformHandle != null)
                    Resize(pos, modifiers.HasFlag(ModifierKeys.Shift));

                if (_selecting && Selection.Count == 0)
                    Select(pos);

                UpdateCursor(pos);

                _lastPosition = pos;
            }
        }

        public void MouseUp(Vector2 pos)
        {
            // do all UI operations out here to avoid deadlock
            // otherwise, we might block on UI operation while
            // UI thread is blocking on us
            var modifiers = App.Dispatcher.Invoke(() => Keyboard.Modifiers);

            using (new WeakLock(this))
            {
                _transformHandle = null;

                if (!_moved)
                {
                    ILayer hit = null;
                    var cache = Context.CacheManager;
                    var shift = modifiers.HasFlag(ModifierKeys.Shift);
                    var alt = modifiers.HasFlag(ModifierKeys.Alt);

                    foreach (var l in Selection.Reverse())
                    {
                        var layer = l;

                        while (true)
                        {
                            if (layer.Parent == null) break;

                            var siblings = layer.Parent.SubLayers;
                            var range = siblings.ToList();

                            if (alt) // cycle the list
                            {
                                if (siblings.Count < 2)
                                {
                                    layer = layer.Parent;
                                    continue;
                                }

                                range = siblings.SkipUntil(s => s != layer)
                                    .Concat(siblings.TakeUntil(s => s != layer))
                                    .ToList();
                            }

                            foreach (var child in range)
                            {
                                hit = child.Hit(cache, pos, false);

                                if (hit != null) break;
                            }

                            if (hit != null) break;

                            layer = layer.Parent;
                        }

                        if (hit != null) break;

                        hit = layer.Hit(cache, pos, false);

                        if (hit != null) break;
                    }

                    if (hit == null)
                        hit = Root.SubLayers.Select(l => l.Hit(cache, pos, true))
                            .FirstOrDefault(l => l != null);

                    if (!shift)
                        ClearSelection();

                    if (hit != null)
                    {
                        hit.Selected = true;
                        _transformHandle = null;
                    }
                }

                if (_selecting)
                {
                    Parallel.ForEach(Root.Flatten(), layer =>
                    {
                        var bounds = Context.CacheManager.GetAbsoluteBounds(layer);
                        layer.Selected = layer.Selected || _selectionBox.Contains(bounds);
                    });

                    Context.InvalidateSurface();
                    _selectionBox = RectangleF.Empty;

                    _selecting = false;
                }
            }
        }

        public void Render(RenderContext target, ICacheManager cache)
        {
            void DrawBounds(RectangleF rect, Matrix3x2 transform, IBrush brush)
            {
                target.Transform(transform);

                using (var pen = target.CreatePen(1, brush))
                {
                    target.DrawRectangle(rect, pen);
                }

                target.Transform(MathUtils.Invert(transform));
            }

            using (new WeakLock(_render))
            {
                if (Selection.Count > 0)
                {
                    var distort =
                        Matrix3x2.CreateTranslation(-SelectionBounds.Center) *
                        Matrix3x2.CreateSkew(0, SelectionShear) *
                        Matrix3x2.CreateTranslation(SelectionBounds.Center) *
                        Matrix3x2.CreateRotation(SelectionRotation, SelectionBounds.Center);

                    foreach (var layer in Selection)
                        if (layer is IGeometricLayer shape)
                        {
                            var geom = Context.CacheManager.GetGeometry(shape);

                            if (geom != null)
                            {
                                target.Transform(shape.AbsoluteTransform);

                                using (var pen = target.CreatePen(1, cache.GetBrush("A1")))
                                {
                                    target.DrawGeometry(geom, pen);
                                }

                                target.Transform(MathUtils.Invert(shape.AbsoluteTransform));
                            }
                        }

                    DrawBounds(SelectionBounds, distort, cache.GetBrush("A2"));
                }

                if (!_selectionBox.IsEmpty)
                {
                    using (var pen = target.CreatePen(1, cache.GetBrush("A1")))
                    {
                        target.DrawRectangle(_selectionBox, pen);
                    }
                    target.FillRectangle(_selectionBox, cache.GetBrush("A1A"));
                }
            }
        }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return MathUtils.Rotate(
                MathUtils.ShearX(v, SelectionBounds.Center, SelectionShear),
                SelectionBounds.Center,
                SelectionRotation);
        }

        public void Transform(Vector2 scale, Vector2 translate,
            float rotate, float shear, Vector2 origin)
        {
            Transform(scale, translate, rotate, shear, origin, false);
        }

        public void Update(bool reset)
        {
            using (new WeakLock(_render))
            {
                InvalidateSurface();

                RectangleF bounds;

                switch (Selection.Count)
                {
                    case 0:
                        bounds = RectangleF.Empty;
                        break;

                    case 1:
                        var layer = Selection[0];
                        var local = Context.CacheManager.GetBounds(layer);
                        var transform = layer.AbsoluteTransform.Decompose();

                        bounds =
                            MathUtils.Bounds(
                                local,
                                Matrix3x2.CreateScale(transform.scale) *
                                Matrix3x2.CreateTranslation(transform.translation));

                        var center = Vector2.Transform(
                            local.Center,
                            layer.AbsoluteTransform);

                        bounds.Offset(center - bounds.Center);

                        if (reset)
                        {
                            SelectionRotation = transform.rotation;
                            SelectionShear = transform.skew;
                        }
                        break;

                    default:
                        bounds =
                            Selection
                                .AsParallel()
                                .Select(l => Context.CacheManager.GetAbsoluteBounds(l))
                                .Aggregate(RectangleF.Union);

                        if (reset)
                        {
                            SelectionRotation = 0;
                            SelectionShear = 0;
                        }
                        break;
                }

                SelectionBounds = bounds;
            }

            InvalidateSurface();
        }

        public IArtContext Context { get; }

        public IBitmap Cursor { get; set; }

        public Group Root => Context.ViewManager.Root;
        public RectangleF SelectionBounds { get; set; }
        public float SelectionRotation { get; set; }
        public float SelectionShear { get; set; }
        IList<Layer> ISelectionManager.Selection => Selection;

        #endregion
    }
}