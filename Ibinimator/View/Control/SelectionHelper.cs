using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using System;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;

namespace Ibinimator.View.Control
{
    internal class SelectionHelper : Model.Model
    {

        #region Fields

        public RectangleF selectionBounds;
        public RectangleF selectionBox;

        public float selectionRotation;
        public float selectionShear;

        private Vector2? lastPosition;
        private bool moved;
        private bool selecting;
        private StrokeStyle1 selectionStroke;
        private object sync = new object();

        #endregion Fields

        #region Constructors

        public SelectionHelper(ArtView artView)
        {
            ArtView = artView;
            ArtView.RenderTargetBound += OnArtViewRenderTargetBound;
        }

        #endregion Constructors

        #region Properties

        public ArtView ArtView { get; }

        public Bitmap Cursor { get; set; }

        public ArtViewHandle? ResizingHandle { get; set; }

        public Model.Layer Root => ArtView.Dispatcher.Invoke(() => ArtView.Root);

        public IList<Model.Layer> Selection { get; set; }

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
                    ResizingHandle = test.handle;
                    bool hit = test.handle != null;

                    if (!hit)
                    {
                        foreach (var l in Selection)
                        {
                            if (l.Hit(factory, pos, l.Parent.AbsoluteTransform) != null)
                            {
                                ResizingHandle = ArtViewHandle.Translation;
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
            lock (this)
            {
                moved = true;

                lastPosition = lastPosition ?? pos;

                float width = Math.Max(1f, selectionBounds.Width),
                    height = Math.Max(1f, selectionBounds.Height);

                if (ResizingHandle != null)
                    Resize(pos);

                UpdateCursor(pos);

                if (selecting && Selection.Count == 0)
                    Select(pos);

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
                ResizingHandle = null;

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
                        var bounds = ArtView.Cache.GetBounds(layer);
                        selectionBox.Contains(ref bounds, out bool contains);
                        layer.Selected = layer.Selected || contains;
                    });

                    ArtView.InvalidateSurface(selectionBox.Inflate(2));
                    selectionBox = RectangleF.Empty;

                    selecting = false;
                }

                Update(false);
            }
        }

        public void Render(RenderTarget target, Brush fill, Brush stroke)
        {
            void DrawBounds(RectangleF rect, Matrix3x2 transform, Brush accent)
            {
                target.Transform *= transform;

                // draw selection outline
                target.DrawRectangle(selectionBounds, fill, 1, selectionStroke);

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
                    target.FillEllipse(e, accent);
                    target.DrawEllipse(e, stroke, 2f / zoom.LengthSquared() * 2);
                }
            }

            Matrix3x2 distort =
                Matrix3x2.Translation(-selectionBounds.TopLeft) *
                Matrix3x2.Skew(0, selectionShear) *
                Matrix3x2.Translation(selectionBounds.TopLeft) *
                Matrix3x2.Rotation(selectionRotation, selectionBounds.Center);

            foreach (var layer in Selection)
            {
                if (layer is Model.Shape shape)
                {
                    target.Transform *= shape.AbsoluteTransform;
                    target.DrawGeometry(ArtView.Cache.GetGeometry(shape), fill, 1f, selectionStroke);
                    target.Transform *= Matrix3x2.Invert(shape.AbsoluteTransform);
                }
            }

            DrawBounds(selectionBounds, distort, fill);

            if (!selectionBox.IsEmpty)
            {
                target.DrawRectangle(selectionBox, fill, 1f / target.Transform.M11);
                fill.Opacity = 0.25f;
                target.FillRectangle(selectionBox, fill);
                fill.Opacity = 1.0f;
            }
            if (Cursor != null)
            {
                target.Transform =
                    Matrix3x2.Scaling(1f / 3) *
                    Matrix3x2.Rotation(selectionRotation - selectionShear, new Vector2(8)) *
                    Matrix3x2.Translation(lastPosition.Value - new Vector2(8));
                target.DrawBitmap(Cursor, 1, BitmapInterpolationMode.Linear);
            }
        }

        public void Update(bool reset)
        {
            InvalidateSurface();

            switch (Selection.Count)
            {
                case 0:
                    selectionBounds = RectangleF.Empty;
                    return;

                case 1:
                    selectionBounds = Selection[0].GetAxisAlignedBounds();

                    if (reset)
                    {
                        selectionRotation = Selection[0].Rotation;
                        selectionShear = Selection[0].Shear;
                    }

                    var delta =
                        MathUtils.Rotate(selectionBounds.Center, selectionBounds.TopLeft, selectionRotation) -
                        selectionBounds.Center;

                    selectionBounds.Offset(delta);
                    break;

                default:
                    RectangleF bounds = ArtView.Cache.GetBounds(Selection[0]);
                    (float x1, float y1, float x2, float y2) = (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);

                    Parallel.ForEach(Selection.Skip(1), l =>
                    {
                        var b = ArtView.Cache.GetBounds(l);

                        if (b.Left < x1) x1 = b.Left;
                        if (b.Top < y1) y1 = b.Top;
                        if (b.Right > x2) x2 = b.Right;
                        if (b.Bottom > y2) y2 = b.Bottom;
                    });

                    selectionBounds = new RectangleF(x1, y1, x2 - x1, y2 - y1);

                    if (reset)
                    {
                        selectionRotation = 0;
                        selectionShear = 0;
                    }
                    break;
            }

            InvalidateSurface();
        }

        private Vector2 FromSelectionSpace(Vector2 v) =>
            MathUtils.ShearX(
                MathUtils.Rotate(v, selectionBounds.Center, -selectionRotation),
                selectionBounds.TopLeft,
                -selectionShear);

        private (Bitmap cursor, ArtViewHandle? handle) HandleTest(Vector2 pos)
        {
            List<(Vector2 pos, string cur, ArtViewHandle handle)> handles = new List<(Vector2, string, ArtViewHandle)>();

            pos = FromSelectionSpace(pos);

            Vector2 tl = selectionBounds.TopLeft,
                br = selectionBounds.BottomRight;

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
                if ((pos - h.pos).LengthSquared() < 25f / ArtView.ViewTransform.ScaleVector.LengthSquared())
                    return (ArtView.Cache.GetBitmap("cursor-" + h.cur), h.handle);
            }

            return (null, null);
        }

        private void InvalidateSurface()
        {
            Matrix3x2 distort =
                Matrix3x2.Translation(-selectionBounds.TopLeft) *
                Matrix3x2.Skew(0, selectionShear) *
                Matrix3x2.Translation(selectionBounds.TopLeft) *
                Matrix3x2.Rotation(selectionRotation, selectionBounds.Center);

            ArtView.InvalidateSurface(
                MathUtils.Bounds(
                    selectionBounds,
                    distort).Inflate(20));
        }

        private void OnArtViewRenderTargetBound(object sender, RenderTarget target)
        {
            selectionStroke = new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), 
                new StrokeStyleProperties1() { TransformType = StrokeTransformType.Hairline });
        }
        private void Resize(Vector2 pos)
        {
            Vector2 scale = Vector2.One;
            Vector2 translate = Vector2.Zero;
            float rotate = 0;

            Vector2 origin = Vector2.Zero;
            Vector2 axis = Vector2.Zero;
            Vector2 rpos = FromSelectionSpace(pos);

            switch (ResizingHandle)
            {
                case ArtViewHandle.Top:
                    origin = new Vector2(selectionBounds.Center.X, selectionBounds.Bottom);
                    axis = new Vector2(0, 1);
                    break;

                case ArtViewHandle.Bottom:
                    origin = new Vector2(selectionBounds.Center.X, selectionBounds.Top);
                    axis = new Vector2(0, -1);
                    break;

                case ArtViewHandle.Left:
                    origin = new Vector2(selectionBounds.Right, selectionBounds.Center.Y);
                    axis = new Vector2(1, 0);
                    break;

                case ArtViewHandle.Right:
                    origin = new Vector2(selectionBounds.Left, selectionBounds.Center.Y);
                    axis = new Vector2(-1, 0);
                    break;

                case ArtViewHandle.TopRight:
                    origin = selectionBounds.BottomLeft;
                    axis = new Vector2(-1, 1);
                    break;

                case ArtViewHandle.TopLeft:
                    origin = selectionBounds.BottomRight;
                    axis = new Vector2(1, 1);
                    break;

                case ArtViewHandle.Translation:
                    translate = pos - lastPosition.Value;
                    break;

                case ArtViewHandle.Rotation:
                    var x = pos - selectionBounds.Center;
                    var r = -(float)(Math.Atan2(-x.Y, x.X) - MathUtil.PiOverTwo);
                    rotate = r - selectionRotation;
                    selectionRotation = r;
                    origin = selectionBounds.Center;
                    break;
            }

            if (axis != Vector2.Zero)
            {
                axis.Y *= selectionBounds.Height / selectionBounds.Width;

                var crossSection = MathUtils.CrossSection(axis, origin, selectionBounds);
                var axisLength = (crossSection.Item2 - crossSection.Item1).Length();

                //origin = ToSelectionSpace(origin);
                //axis = Vector2.Normalize(MathUtils.Rotate(MathUtils.ShearX(axis, selectionShear), selectionRotation));
                axis = Vector2.Normalize(axis);
                scale =
                    MathUtils.Project(rpos - origin, axis) * -MathUtils.Sign(axis) / axisLength +
                    Vector2.One - MathUtils.Abs(axis);

                if (float.IsNaN(scale.X) || float.IsNaN(scale.Y)) Debugger.Break();

                // don't let them scale to 0, otherwise we can't scale back
                // because 0 x 0 = 0
                scale.X = MathUtils.AbsMax(0.001f, scale.X);
                scale.Y = MathUtils.AbsMax(0.001f, scale.Y);

                if (scale.X < 0)
                {
                    switch (ResizingHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            ResizingHandle = ArtViewHandle.TopRight;
                            break;

                        case ArtViewHandle.TopRight:
                            ResizingHandle = ArtViewHandle.TopLeft;
                            break;

                        case ArtViewHandle.Left:
                            ResizingHandle = ArtViewHandle.Right;
                            break;

                        case ArtViewHandle.Right:
                            ResizingHandle = ArtViewHandle.Left;
                            break;

                        case ArtViewHandle.BottomLeft:
                            ResizingHandle = ArtViewHandle.BottomRight;
                            break;

                        case ArtViewHandle.BottomRight:
                            ResizingHandle = ArtViewHandle.BottomLeft;
                            break;
                    }
                }

                if (scale.Y < 0)
                {
                    switch (ResizingHandle)
                    {
                        case ArtViewHandle.TopLeft:
                            ResizingHandle = ArtViewHandle.BottomLeft;
                            break;

                        case ArtViewHandle.TopRight:
                            ResizingHandle = ArtViewHandle.BottomRight;
                            break;

                        case ArtViewHandle.Top:
                            ResizingHandle = ArtViewHandle.Bottom;
                            break;

                        case ArtViewHandle.Bottom:
                            ResizingHandle = ArtViewHandle.Top;
                            break;

                        case ArtViewHandle.BottomLeft:
                            ResizingHandle = ArtViewHandle.TopLeft;
                            break;

                        case ArtViewHandle.BottomRight:
                            ResizingHandle = ArtViewHandle.TopRight;
                            break;
                    }
                }
            }

            Matrix3x2 transform =
                Matrix3x2.Rotation(-selectionRotation, selectionBounds.Center) *
                Matrix3x2.Translation(-selectionBounds.TopLeft) *
                Matrix3x2.Skew(0, -selectionShear) *
                Matrix3x2.Scaling(scale.X, scale.Y, origin - selectionBounds.TopLeft) *
                Matrix3x2.Skew(0, selectionShear) *
                Matrix3x2.Translation(selectionBounds.TopLeft) *
                Matrix3x2.Rotation(selectionRotation, selectionBounds.Center) *
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

            Update(false);
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
                MathUtils.ShearX(v, selectionBounds.TopLeft, selectionShear), 
                selectionBounds.Center, 
                selectionRotation);
        private void UpdateCursor(Vector2 pos)
        {
            if (ResizingHandle == null)
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