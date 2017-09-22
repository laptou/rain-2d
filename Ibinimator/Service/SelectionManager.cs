using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Shared;
using System.Linq;
using System.Windows.Input;
using Ibinimator.Direct2D;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

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


    public class SelectionManager : Model.Model, ISelectionManager
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
            ArtView = artView;
            ArtView.RenderTargetBound += OnRenderTargetBound;

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
                    case nameof(SharpDX.Direct2D1.Layer.Selected):
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

        private (Bitmap cursor, SelectionResizeHandle? handle) HandleTest(Vector2 pos)
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
            handles.Add((new Vector2((x1 + x2) / 2, y1 - 10), "rot", SelectionResizeHandle.Rotation));

            foreach (var h in handles)
                if ((pos - h.pos).LengthSquared() < 49 / ArtView.ViewManager.Zoom)
                    return (ArtView.CacheManager.GetBitmap("cursor-" + h.cur), h.handle);

            return (null, null);
        }

        private void InvalidateSurface()
        {
            ArtView.InvalidateSurface();
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
                    var angle = (float) Math.Atan2(x.Y, x.X) + MathUtil.PiOverTwo;
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

            if (Math.Abs(rotate) > MathUtil.PiOverFour && uniform)
                rotate = Math.Sign(rotate) * MathUtil.PiOverFour;

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
            ArtView.InvalidateSurface();

            if (pos.X < _selectionBox.Left)
                _selectionBox.Left = pos.X;
            else
                _selectionBox.Right = pos.X;

            if (pos.Y < _selectionBox.Top)
                _selectionBox.Top = pos.Y;
            else
                _selectionBox.Bottom = pos.Y;

            ArtView.InvalidateSurface();
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
                Matrix3x2.Rotation(-SelectionRotation, SelectionBounds.Center) *
                Matrix3x2.Translation(-SelectionBounds.Center) *
                Matrix3x2.Skew(0, -SelectionShear) *
                Matrix3x2.Scaling(scale.X, scale.Y, origin - SelectionBounds.Center) *
                Matrix3x2.Skew(0, SelectionShear + shear) *
                Matrix3x2.Translation(SelectionBounds.Center) *
                Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center) *
                Matrix3x2.Rotation(rotate, so) *
                Matrix3x2.Translation(translate);

            var history = ArtView.HistoryManager;

            if (continuous && history.Current is TransformCommand lastTransformCommand)
            {
                var current =
                    new TransformCommand(
                        0,
                        Selection.ToArray<ILayer>(),
                        transform);

                current.Do(ArtView);

                history.Replace(
                    new TransformCommand(
                        ArtView.HistoryManager.Position,
                        Selection.ToArray<ILayer>(),
                        lastTransformCommand.Transform * transform));
            }
            else
            {
                var current = new TransformCommand(
                    ArtView.HistoryManager.Position + 1,
                    Selection.ToArray<ILayer>(), transform);

                history.Do(current);
            }

            var sb = SelectionBounds;
            var tl = MathUtils.Scale(sb.TopLeft, origin, scale);
            var br = MathUtils.Scale(sb.BottomRight, origin, scale);
            (tl, br) = (Vector2.Min(tl, br), Vector2.Max(tl, br));

            sb = new RectangleF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);

            var newCenter =
                Matrix3x2.TransformPoint(
                    transform, SelectionBounds.Center);
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

            ArtView.InvalidateSurface();
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

                if (Selection.Count > 0 && ArtView.ToolManager.Type == ToolType.Select)
                {
                    _transformHandle = HandleTest(pos).handle;

                    if (_transformHandle == null && Selection.Any(
                            l =>
                            {
                                var pt =
                                    Matrix3x2.TransformPoint(
                                        Matrix3x2.Invert(
                                            l.WorldTransform),
                                        pos);

                                return l.Hit(ArtView.CacheManager, pt, true) != null;
                            }))
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
            var modifiers = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

            using (new WeakLock(this))
            {
                if (!_moved && _transformHandle != null)
                    ArtView.HistoryManager.Do(
                        new TransformCommand(
                            ArtView.HistoryManager.Position + 1,
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
            var modifiers = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

            using (new WeakLock(this))
            {
                _transformHandle = null;

                if (!_moved)
                {
                    ILayer hit = null;
                    var cache = ArtView.CacheManager;
                    var shift = modifiers.HasFlag(ModifierKeys.Shift);
                    var alt = modifiers.HasFlag(ModifierKeys.Alt);


                    if (shift)
                        hit = Selection.Select(l => l.Parent)
                            .Select(g => g.Hit(cache, pos, false))
                            .FirstOrDefault(l => l != null);

                    if (hit == null)
                        hit = Selection.OfType<Group>()
                            .Select(g => g.Hit(cache, pos, false))
                            .FirstOrDefault(l => l != null);

                    if (hit == null)
                        hit = Root.SubLayers.Select(l => l.Hit(cache, pos, !alt))
                            .FirstOrDefault(l => l != null);

                    if (!modifiers.HasFlag(ModifierKeys.Shift))
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
                        var bounds = ArtView.CacheManager.GetAbsoluteBounds(layer);
                        _selectionBox.Contains(ref bounds, out var contains);
                        layer.Selected = layer.Selected || contains;
                    });

                    ArtView.InvalidateSurface();
                    _selectionBox = RectangleF.Empty;

                    _selecting = false;
                }
            }
        }

        public void Render(RenderTarget target, ICacheManager cache)
        {
            void DrawBounds(RectangleF rect, Matrix3x2 transform, Brush brush)
            {
                target.Transform *= transform;

                target.DrawRectangle(SelectionBounds, brush, 1, _selectionStroke);

                target.Transform *= Matrix3x2.Invert(transform);
            }

            using (new WeakLock(_render))
            {
                if (Selection.Count > 0)
                {
                    var distort =
                        Matrix3x2.Translation(-SelectionBounds.Center) *
                        Matrix3x2.Skew(0, SelectionShear) *
                        Matrix3x2.Translation(SelectionBounds.Center) *
                        Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center);

                    foreach (var layer in Selection)
                        if (layer is IGeometricLayer shape)
                        {
                            var geom = ArtView.CacheManager.GetGeometry(shape);

                            if (geom != null)
                            {
                                target.Transform = shape.AbsoluteTransform * target.Transform;
                                target.DrawGeometry(geom, cache.GetBrush("A1"), 1f,
                                    _selectionStroke);
                                target.Transform = Matrix3x2.Invert(shape.AbsoluteTransform) * target.Transform;
                            }
                        }

                    DrawBounds(SelectionBounds, distort, cache.GetBrush("A1"));
                }

                if (!_selectionBox.IsEmpty)
                {
                    target.DrawRectangle(_selectionBox, cache.GetBrush("A1"), 1f / target.Transform.M11);
                    target.FillRectangle(_selectionBox, cache.GetBrush("A1-1/2"));
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
                        var transform = Selection[0].AbsoluteTransform.Decompose();
                        var local = ArtView.CacheManager.GetBounds(Selection[0]);

                        bounds =
                            MathUtils.Bounds(
                                local,
                                Matrix3x2.Scaling(transform.scale) *
                                Matrix3x2.Translation(transform.translation));

                        var center = Matrix3x2.TransformPoint(
                            Selection[0].AbsoluteTransform,
                            local.Center);

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
                                .Select(l => ArtView.CacheManager.GetAbsoluteBounds(l))
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

        public ArtView ArtView { get; }

        public Bitmap Cursor { get; set; }

        public Group Root => ArtView.ViewManager.Root;
        public RectangleF SelectionBounds { get; set; }
        public float SelectionRotation { get; set; }
        public float SelectionShear { get; set; }
        IList<Layer> ISelectionManager.Selection => Selection;

        #endregion
    }
}