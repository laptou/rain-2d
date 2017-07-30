using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.Collections.Generic;

using System.Linq;

using System.Linq;
using System;

using System.ComponentModel;
using System.Diagnostics;

using System.Linq;
using System.Threading.Tasks;

using System.Windows.Input;

namespace Ibinimator.View.Control
{
    internal class SelectionManager : Model.Model, ISelectionManager
    {

        #region Fields

        private object _render = new object();
        private Vector2? lastPosition;
        private bool moved;
        private ArtViewHandle? resizingHandle;
        private bool selecting;
        private RectangleF selectionBox;
        private StrokeStyle1 selectionStroke;

        #endregion Fields

        #region Constructors

        public SelectionManager(ArtView artView)
        {
            ArtView = artView;
            ArtView.RenderTargetBound += OnRenderTargetBound;
        }

        #endregion Constructors

        #region Events

        public event EventHandler Updated;

        #endregion Events

        #region Properties

        public ArtView ArtView { get; }
        public Bitmap Cursor { get; set; }
        public Model.Layer Root => ArtView.ViewManager.Root;
        public IList<Model.Layer> Selection { get; set; }
        public RectangleF SelectionBounds { get; set; }
        public float SelectionRotation { get; set; }
        public float SelectionShear { get; set; }

        #endregion Properties

        #region Methods

        public void ClearSelection()
        {
            while (Selection.Count > 0)
                Selection[0].Selected = false;
        }

        public void OnMouseDown(Vector2 pos, Factory factory)
        {
            lock (this)
            {
                moved = false;

                if (Selection.Count > 0)
                {
                    var test = HandleTest(pos);
                    resizingHandle = test.handle;
                    bool hit = test.handle != null;

                    if (!hit)
                    {
                        foreach (var l in Selection)
                        {
                            if (l.Hit(factory, pos, l.Parent.AbsoluteTransform) != null)
                            {
                                resizingHandle = ArtViewHandle.Translation;
                                hit = true;
                                break;
                            }
                        }
                    }

                    if (!hit && !ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers).HasFlag(ModifierKeys.Shift))
                        ClearSelection();
                }

                if (Selection.Count == 0 && !selecting)
                {
                    selectionBox = new RectangleF(pos.X, pos.Y, 0, 0);
                    selecting = true;
                }

                lastPosition = pos;

                Update(false);
            }
        }

        public void OnMouseMove(Vector2 pos, Factory factory)
        {
            var modifiers = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

            lock (this)
            {
                moved = true;

                lastPosition = lastPosition ?? pos;

                float width = Math.Max(1f, SelectionBounds.Width),
                    height = Math.Max(1f, SelectionBounds.Height);

                if (resizingHandle != null)
                    Resize(pos, modifiers.HasFlag(ModifierKeys.Shift));

                if (selecting && Selection.Count == 0)
                    Select(pos);

                UpdateCursor(pos);

                lastPosition = pos;
            }
        }

        public void OnMouseUp(Vector2 pos, Factory factory)
        {
            // do all UI operations out here to avoid deadlock
            // otherwise, we might block on UI operation while
            // UI thread is blocking on us
            var modifiers = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

            lock (this)
            {
                resizingHandle = null;

                if (!moved)
                {
                    var hit = Root.Hit(factory, pos, Matrix3x2.Identity);

                    if (!modifiers.HasFlag(ModifierKeys.Shift))
                        ClearSelection();

                    if (hit != null)
                        hit.Selected = true;
                }

                if (selecting)
                {
                    Parallel.ForEach(Root.Flatten(), layer =>
                    {
                        var bounds = ArtView.CacheManager.GetBounds(layer);
                        selectionBox.Contains(ref bounds, out bool contains);
                        layer.Selected = layer.Selected || contains;
                    });

                    ArtView.InvalidateSurface(selectionBox.Inflate(2));
                    selectionBox = RectangleF.Empty;

                    selecting = false;
                }
            }
        }

        public void Render(RenderTarget target, ICacheManager cache)
        {
            void DrawBounds(RectangleF rect, Matrix3x2 transform)
            {
                target.Transform *= transform;

                // draw selection outline
                target.DrawRectangle(SelectionBounds, cache.GetBrush("A1"), 1, selectionStroke);

                target.Transform *= Matrix3x2.Invert(transform);

                // draw handles
                List<Vector2> handles = new List<Vector2>();

                float x1 = rect.Left, y1 = rect.Top,
                    x2 = rect.Right, y2 = rect.Bottom;

                handles.Add(new Vector2(x1, y1));
                handles.Add(new Vector2(x2, y1));
                handles.Add(new Vector2(x2, y2));
                handles.Add(new Vector2(x1, y2));
                handles.Add(new Vector2((x1 + x2) / 2, y1));
                handles.Add(new Vector2(x1, (y1 + y2) / 2));
                handles.Add(new Vector2(x2, (y1 + y2) / 2));
                handles.Add(new Vector2((x1 + x2) / 2, y2));
                handles.Add(new Vector2((x1 + x2) / 2, y1 - 10));

                var zoom = MathUtils.GetScale(target.Transform);

                foreach (Vector2 v in handles.Select(ToSelectionSpace))
                {
                    Ellipse e = new Ellipse(v, 5f / zoom.Y, 5f / zoom.X);
                    target.FillEllipse(e, cache.GetBrush("A1"));
                    target.DrawEllipse(e, cache.GetBrush("L1"), 2f / zoom.LengthSquared() * 2);
                }
            }

            lock (_render)
            {
                if (Selection.Count > 0)
                {
                    Matrix3x2 distort =
                        Matrix3x2.Translation(-SelectionBounds.TopLeft) *
                        Matrix3x2.Skew(0, SelectionShear) *
                        Matrix3x2.Translation(SelectionBounds.TopLeft) *
                        Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center);

                    foreach (var layer in Selection)
                    {
                        if (layer is Model.Shape shape)
                        {
                            target.Transform *= shape.AbsoluteTransform;
                            target.DrawGeometry(ArtView.CacheManager.GetGeometry(shape), cache.GetBrush("A1"), 1f, selectionStroke);
                            target.Transform *= Matrix3x2.Invert(shape.AbsoluteTransform);
                        }
                    }

                    DrawBounds(SelectionBounds, distort);
                }

                if (!selectionBox.IsEmpty)
                {
                    target.DrawRectangle(selectionBox, cache.GetBrush("A1"), 1f / target.Transform.M11);
                    target.FillRectangle(selectionBox, cache.GetBrush("A1-1/2"));
                }

                if (Cursor != null)
                {
                    target.Transform =
                        Matrix3x2.Scaling(1f / 3) *
                        Matrix3x2.Rotation(SelectionRotation - SelectionShear, new Vector2(8)) *
                        Matrix3x2.Translation(lastPosition.Value - new Vector2(8));
                    target.DrawBitmap(Cursor, 1, BitmapInterpolationMode.Linear);
                }
            }
        }

        public void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin)
        {
            origin *= new Vector2(SelectionBounds.Width, SelectionBounds.Height);
            origin += SelectionBounds.TopLeft;

            Matrix3x2 transform =
               Matrix3x2.Rotation(-SelectionRotation, SelectionBounds.Center) *
               Matrix3x2.Translation(-SelectionBounds.TopLeft) *
               Matrix3x2.Skew(0, -SelectionShear) *
               Matrix3x2.Scaling(scale.X, scale.Y, origin - SelectionBounds.TopLeft) *
               Matrix3x2.Skew(0, SelectionShear) *
               Matrix3x2.Translation(SelectionBounds.TopLeft) *
               Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center) *
               Matrix3x2.Rotation(rotate, origin) *
               Matrix3x2.Translation(translate);

            foreach (var layer in Selection)
            {
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform * transform * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();

                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;

                    layer.UpdateTransform();
                }
            }

            var tl = MathUtils.Scale(SelectionBounds.TopLeft, origin, scale);
            var br = MathUtils.Scale(SelectionBounds.BottomRight, origin, scale);
            var sb = new RectangleF(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);

            var sdelta = Matrix3x2.TransformPoint(transform, SelectionBounds.Center) - sb.Center;
            sb.Offset(sdelta);
            SelectionBounds = sb;

            Updated?.BeginInvoke(this, null, null, null);
            InvalidateSurface();
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

                        if (reset)
                        {
                            SelectionRotation = Selection[0].Rotation;
                            SelectionShear = Selection[0].Shear;
                        }

                        var delta =
                            MathUtils.Rotate(bounds.Center, bounds.TopLeft, SelectionRotation) -
                            bounds.Center;

                        bounds.Offset(delta);
                        break;

                    default:
                        bounds = ArtView.CacheManager.GetBounds(Selection[0]);
                        (float x1, float y1, float x2, float y2) = (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

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

                Updated?.BeginInvoke(this, null, null, null);
            }
        }

        private Vector2 FromSelectionSpace(Vector2 v) =>
            MathUtils.ShearX(
                MathUtils.Rotate(v, SelectionBounds.Center, -SelectionRotation),
                SelectionBounds.TopLeft,
                -SelectionShear);

        private (Bitmap cursor, ArtViewHandle? handle) HandleTest(Vector2 pos)
        {
            List<(Vector2 pos, string cur, ArtViewHandle handle)> handles = new List<(Vector2, string, ArtViewHandle)>();

            pos = FromSelectionSpace(pos);

            Vector2 tl = SelectionBounds.TopLeft,
                br = SelectionBounds.BottomRight;

            float x1 = tl.X, y1 = tl.Y,
                x2 = br.X, y2 = br.Y;

            handles.Add((new Vector2(x1, y1), "nwse", ArtViewHandle.TopLeft));
            handles.Add((new Vector2(x2, y1), "nesw", ArtViewHandle.TopRight));
            handles.Add((new Vector2(x2, y2), "nwse", ArtViewHandle.BottomRight));
            handles.Add((new Vector2(x1, y2), "nesw", ArtViewHandle.BottomLeft));
            handles.Add((new Vector2((x1 + x2) / 2, y1), "ns", ArtViewHandle.Top));
            handles.Add((new Vector2(x1, (y1 + y2) / 2), "ew", ArtViewHandle.Left));
            handles.Add((new Vector2(x2, (y1 + y2) / 2), "ew", ArtViewHandle.Right));
            handles.Add((new Vector2((x1 + x2) / 2, y2), "ns", ArtViewHandle.Bottom));
            handles.Add((new Vector2((x1 + x2) / 2, y1 - 10), "rot", ArtViewHandle.Rotation));

            foreach (var h in handles)
            {
                if ((pos - h.pos).LengthSquared() < 49 / ArtView.ViewManager.Zoom)
                    return (ArtView.CacheManager.GetBitmap("cursor-" + h.cur), h.handle);
            }

            return (null, null);
        }

        private void InvalidateSurface()
        {
            Matrix3x2 distort =
                Matrix3x2.Translation(-SelectionBounds.TopLeft) *
                Matrix3x2.Skew(0, SelectionShear) *
                Matrix3x2.Translation(SelectionBounds.TopLeft) *
                Matrix3x2.Rotation(SelectionRotation, SelectionBounds.Center);

            ArtView.InvalidateSurface(
                MathUtils.Bounds(
                    SelectionBounds,
                    distort).Inflate(20));
        }

        private void OnRenderTargetBound(object sender, RenderTarget target)
        {
            selectionStroke = new StrokeStyle1(target.Factory.QueryInterface<Factory1>(),
                new StrokeStyleProperties1() { TransformType = StrokeTransformType.Hairline });
        }

        private void Resize(Vector2 pos, bool uniform)
        {
            Vector2 scale = Vector2.One;
            Vector2 translate = Vector2.Zero;
            float rotate = 0;

            Vector2 origin = Vector2.Zero;
            Vector2 axis = Vector2.Zero;
            Vector2 rpos = FromSelectionSpace(pos);

            switch (resizingHandle)
            {
                case ArtViewHandle.Top:
                    origin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Bottom);
                    axis = new Vector2(0, 1);
                    break;

                case ArtViewHandle.Bottom:
                    origin = new Vector2(SelectionBounds.Center.X, SelectionBounds.Top);
                    axis = new Vector2(0, -1);
                    break;

                case ArtViewHandle.Left:
                    origin = new Vector2(SelectionBounds.Right, SelectionBounds.Center.Y);
                    axis = new Vector2(1, 0);
                    break;

                case ArtViewHandle.Right:
                    origin = new Vector2(SelectionBounds.Left, SelectionBounds.Center.Y);
                    axis = new Vector2(-1, 0);
                    break;

                case ArtViewHandle.TopRight:
                    origin = SelectionBounds.BottomLeft;
                    axis = new Vector2(-1, 1);
                    break;

                case ArtViewHandle.TopLeft:
                    origin = SelectionBounds.BottomRight;
                    axis = new Vector2(1, 1);
                    break;

                case ArtViewHandle.Translation:
                    translate = pos - lastPosition.Value;
                    break;

                case ArtViewHandle.Rotation:
                    var x = pos - SelectionBounds.Center;
                    var r = -(float)(Math.Atan2(-x.Y, x.X) - MathUtil.PiOverTwo);
                    rotate = r - SelectionRotation;
                    SelectionRotation = r;
                    origin = SelectionBounds.Center;
                    // origin.Y -= selectionBounds.Height / 2 * (float)Math.Sin(selectionShear) / 2;
                    break;
            }

            if (axis != Vector2.Zero)
            {
                axis.Y *= SelectionBounds.Height / SelectionBounds.Width;

                var crossSection = MathUtils.CrossSection(axis, origin, SelectionBounds);
                var axisLength = (crossSection.Item2 - crossSection.Item1).Length();

                //origin = ToSelectionSpace(origin);
                //axis = Vector2.Normalize(MathUtils.Rotate(MathUtils.ShearX(axis, selectionShear), selectionRotation));
                axis = Vector2.Normalize(axis);

                if (uniform)
                {
                    scale =
                        MathUtils.Project(rpos - origin, axis) *
                        -MathUtils.Sign(axis) / axisLength +
                        Vector2.One - MathUtils.Abs(axis);
                }
                else
                {
                    scale =
                        (rpos - origin) *
                        -MathUtils.Sign(axis) / axisLength +
                        Vector2.One - MathUtils.Abs(axis);
                }

                if (float.IsNaN(scale.X) || float.IsNaN(scale.Y)) Debugger.Break();

                // don't let them scale to 0, otherwise we can't scale back
                // because 0 x 0 = 0
                scale.X = MathUtils.AbsMax(0.001f, scale.X);
                scale.Y = MathUtils.AbsMax(0.001f, scale.Y);

                if (scale.X < 0)
                {
                    switch (resizingHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            resizingHandle = ArtViewHandle.TopRight;
                            break;

                        case ArtViewHandle.TopRight:
                            resizingHandle = ArtViewHandle.TopLeft;
                            break;

                        case ArtViewHandle.Left:
                            resizingHandle = ArtViewHandle.Right;
                            break;

                        case ArtViewHandle.Right:
                            resizingHandle = ArtViewHandle.Left;
                            break;

                        case ArtViewHandle.BottomLeft:
                            resizingHandle = ArtViewHandle.BottomRight;
                            break;

                        case ArtViewHandle.BottomRight:
                            resizingHandle = ArtViewHandle.BottomLeft;
                            break;
                    }
                }

                if (scale.Y < 0)
                {
                    switch (resizingHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            resizingHandle = ArtViewHandle.BottomLeft;
                            break;

                        case ArtViewHandle.TopRight:
                            resizingHandle = ArtViewHandle.BottomRight;
                            break;

                        case ArtViewHandle.Top:
                            resizingHandle = ArtViewHandle.Bottom;
                            break;

                        case ArtViewHandle.Bottom:
                            resizingHandle = ArtViewHandle.Top;
                            break;

                        case ArtViewHandle.BottomLeft:
                            resizingHandle = ArtViewHandle.TopLeft;
                            break;

                        case ArtViewHandle.BottomRight:
                            resizingHandle = ArtViewHandle.TopRight;
                            break;
                    }
                }
            }

            Transform(
                scale, 
                translate, 
                rotate, 
                0, 
                (origin - SelectionBounds.TopLeft) / 
                    new Vector2(SelectionBounds.Width, SelectionBounds.Height));
        }

        private void Select(Vector2 pos)
        {
            ArtView.InvalidateSurface(selectionBox.Inflate(2));

            if (pos.X < selectionBox.Left)
                selectionBox.Left = pos.X;
            else
                selectionBox.Right = pos.X;

            if (pos.Y < selectionBox.Top)
                selectionBox.Top = pos.Y;
            else
                selectionBox.Bottom = pos.Y;

            ArtView.InvalidateSurface(selectionBox.Inflate(2));
        }

        private Vector2 ToSelectionSpace(Vector2 v) =>
            MathUtils.Rotate(
                MathUtils.ShearX(v, SelectionBounds.TopLeft, SelectionShear),
                SelectionBounds.Center,
                SelectionRotation);

        private void UpdateCursor(Vector2 pos)
        {
            if (resizingHandle == null)
            {
                if (Selection.Count > 0)
                    Cursor = HandleTest(pos).cursor;
                else Cursor = null;
            }

            ArtView.InvalidateSurface(new RectangleF(lastPosition.Value.X - 12, lastPosition.Value.Y - 12, 24, 24));

            if (Cursor == null) return;
            else
            {
                var vpos = new Vector2(
                    pos.X,
                    pos.Y);

                ArtView.InvalidateSurface(new RectangleF(vpos.X - 12, vpos.Y - 12, 24, 24));
            }
        }

        #endregion Methods
    }
}