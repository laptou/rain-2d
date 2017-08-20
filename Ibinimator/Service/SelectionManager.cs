using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Ibinimator.Shared;
using System.Windows.Input;
using Ibinimator.Model;
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
        private Matrix3x2 _accumulatedTransform;
        private Vector2 _accumulatedTranslation;
        private Vector2? _beginPosition;
        private Vector2? _lastPosition;
        private bool _moved;
        private SelectionResizeHandle? _resizingHandle;
        private bool _selecting;
        private RectangleF _selectionBox;
        private StrokeStyle1 _selectionStroke;
        private Watcher<Guid, Layer> _watcher;

        public SelectionManager(ArtView artView, IViewManager viewManager, IHistoryManager historyManager)
        {
            ArtView = artView;
            ArtView.RenderTargetBound += OnRenderTargetBound;

            Selection = new ObservableCollection<Layer>();
            Selection.CollectionChanged += (sender, args) =>
            {
                Update(true);
                Updated?.Invoke(this, null);
            };

            viewManager.LayerUpdated += (sender, args) =>
            {
                var layer = (Layer) sender;
                if (args.PropertyName == nameof(Layer.Selected))
                {
                    var contains = Selection.Contains(layer);

                    if (layer.Selected && !contains)
                        Selection.Add(layer);
                    else if (!layer.Selected && contains)
                        Selection.Remove(layer);
                }
            };

            historyManager.TimeChanged += (sender, args) =>
            {
                Update(true);
                Updated?.Invoke(this, null);
            };
        }

        public ObservableCollection<Layer> Selection { get; }

        #region ISelectionManager Members

        public event EventHandler Updated;

        public ArtView ArtView { get; }
        public Bitmap Cursor { get; set; }
        public Layer Root => ArtView.ViewManager.Root;
        IList<Layer> ISelectionManager.Selection => Selection;
        public RectangleF SelectionBounds { get; set; }
        public float SelectionRotation { get; set; }
        public float SelectionShear { get; set; }

        public void ClearSelection()
        {
            while (Selection.Count > 0)
                Selection[0].Selected = false;
        }

        public void MouseDown(Vector2 pos)
        {
            lock (this)
            {
                _moved = false;

                if (Selection.Count > 0)
                {
                    var test = HandleTest(pos);
                    _resizingHandle = test.handle;
                    var hit = test.handle != null;

                    if (!hit)
                        foreach (var l in Selection)
                            if (l.Hit(ArtView.RenderTarget.Factory, pos, l.Parent.AbsoluteTransform) != null)
                            {
                                _resizingHandle = SelectionResizeHandle.Translation;
                                hit = true;
                                break;
                            }

                    if (!hit && !ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers).HasFlag(ModifierKeys.Shift))
                        ClearSelection();
                }

                if (Selection.Count == 0 && !_selecting)
                {
                    _selectionBox = new RectangleF(pos.X, pos.Y, 0, 0);
                    _selecting = true;
                }

                _lastPosition = pos;
                _beginPosition = pos;
                _accumulatedTransform = Matrix.Identity;

                var history = ArtView.HistoryManager;
                _watcher = history.BeginRecord(Root);
                Update(false);
            }
        }

        public void MouseMove(Vector2 pos)
        {
            var modifiers = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

            lock (this)
            {
                _moved = true;

                _lastPosition = _lastPosition ?? pos;

                float width = Math.Max(1f, SelectionBounds.Width),
                    height = Math.Max(1f, SelectionBounds.Height);

                if (_resizingHandle != null)
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

            lock (this)
            {
                if (_accumulatedTransform != Matrix3x2.Identity)
                {
                    _resizingHandle = null;

                    var history = ArtView.HistoryManager;
                    history.Key(history.EndRecord(_watcher, history.NextId));
                }

                if (!_moved)
                {
                    var hit = Root.Hit(ArtView.RenderTarget.Factory, pos, Matrix3x2.Identity);

                    if (!modifiers.HasFlag(ModifierKeys.Shift))
                        ClearSelection();

                    if (hit != null)
                        hit.Selected = true;
                }

                if (_selecting)
                {
                    Parallel.ForEach(Root.Flatten(), layer =>
                    {
                        var bounds = ArtView.CacheManager.GetBounds(layer);
                        _selectionBox.Contains(ref bounds, out bool contains);
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

            lock (_render)
            {
                if (Selection.Count > 0)
                {
                    var distort =
                        Matrix3x2.Translation(-SelectionBounds.Center) *
                        Matrix3x2.Skew(0, SelectionShear) *
                        Matrix3x2.Translation(SelectionBounds.Center) *
                        Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center);

                    foreach (var layer in Selection)
                        if (layer is Shape shape)
                        {
                            target.Transform *= shape.AbsoluteTransform;
                            target.DrawGeometry(ArtView.CacheManager.GetGeometry(shape), cache.GetBrush("A1"), 1f,
                                _selectionStroke);
                            target.Transform *= Matrix3x2.Invert(shape.AbsoluteTransform);
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

        public void Transform(Vector2 scale, Vector2 translate,
            float rotate, float shear, Vector2 origin)
        {
            Transform(scale, translate, rotate, shear, origin, true);
        }

        public void Update(bool reset)
        {
            lock (_render)
            {
                InvalidateSurface();

                RectangleF bounds;

                switch (Selection.Count)
                {
                    case 0:
                        bounds = RectangleF.Empty;
                        break;

                    case 1:
                        bounds = Selection[0].GetAxisAlignedBounds();
                        var transform = Selection[0].AbsoluteTransform.Decompose();
                        var origin = transform.translation;

                        if (reset)
                        {
                            SelectionRotation = transform.rotation;
                            SelectionShear = transform.skew;
                        }

                        var delta =
                            MathUtils.Rotate(
                                MathUtils.ShearX(
                                    bounds.Center,
                                    origin,
                                    SelectionShear),
                                origin,
                                SelectionRotation) -
                            bounds.Center;

                        bounds.Offset(delta);
                        break;

                    default:
                        bounds = ArtView.CacheManager.GetBounds(Selection[0]);
                        (float x1, float y1, float x2, float y2) =
                            (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

                        Parallel.ForEach(Selection.Skip(1), l =>
                        {
                            var b = ArtView.CacheManager.GetBounds(l);

                            if (b.Left < x1) x1 = b.Left;
                            if (b.Top < y1) y1 = b.Top;
                            if (b.Right > x2) x2 = b.Right;
                            if (b.Bottom > y2) y2 = b.Bottom;
                        });

                        bounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

                        if (reset)
                        {
                            SelectionRotation = 0;
                            SelectionShear = 0;
                        }
                        break;
                }

                SelectionBounds = bounds;

                InvalidateSurface();
            }
        }

        public Vector2 FromSelectionSpace(Vector2 v)
        {
            return MathUtils.ShearX(
                MathUtils.Rotate(v, SelectionBounds.Center, -SelectionRotation),
                SelectionBounds.Center,
                -SelectionShear);
        }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return MathUtils.Rotate(
                MathUtils.ShearX(v, SelectionBounds.Center, SelectionShear),
                SelectionBounds.Center,
                SelectionRotation);
        }

        #endregion

        private Matrix3x2 Transform(Vector2 scale, Vector2 translate, float rotate,
            float shear, Vector2 origin, bool makeRecord)
        {
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

            if (makeRecord)
            {
                var history = ArtView.HistoryManager;
                _watcher = history.BeginRecord(Root);
            }

            foreach (var layer in Selection)
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform * transform * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();

                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;
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

            if (makeRecord)
            {
                var history = ArtView.HistoryManager;
                history.Key(history.EndRecord(_watcher, history.NextId));
            }

            Updated?.Invoke(this, null);
            InvalidateSurface();

            return transform;
        }

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

            switch (_resizingHandle)
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
                    rotate = angle;
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
                _resizingHandle = _resizingHandle ^ SelectionResizeHandle.Left ^ SelectionResizeHandle.Right;

            if (scale.Y < 0)
                _resizingHandle = _resizingHandle ^ SelectionResizeHandle.Top ^ SelectionResizeHandle.Bottom;

            var relativeOrigin = (origin - SelectionBounds.TopLeft)
                                 / MathUtils.Abs(new Vector2(SelectionBounds.Width, SelectionBounds.Height));

            _accumulatedTranslation += translate;

            _accumulatedTransform *=
                Transform(
                    scale,
                    translate,
                    rotate,
                    0,
                    relativeOrigin,
                    false);
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

        private void UpdateCursor(Vector2 pos)
        {
            if (_resizingHandle == null)
                if (Selection.Count > 0)
                    Cursor = HandleTest(pos).cursor;
                else Cursor = null;

            ArtView.InvalidateSurface();

            if (Cursor == null) return;

            var vpos = new Vector2(
                pos.X,
                pos.Y);

            ArtView.InvalidateSurface();
        }
    }
}