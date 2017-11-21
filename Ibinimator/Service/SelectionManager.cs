using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Windows.Input;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Direct2D;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service
{
    public enum SelectionHandle
    {
        Scale = 1 << -3,
        Rotation = 1 << -2,
        Translation = 1 << -1,
        Top = 1 << 0 | Scale,
        Left = 1 << 1 | Scale,
        Right = 1 << 2 | Scale,
        Bottom = 1 << 3 | Scale,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right
    }

    public sealed class SelectionManager : Core.Model.Model, ISelectionManager
    {
        private string _cursor;
        private SelectionHandle _handle = 0;
        private (bool alt, bool shift, bool ctrl) _modifiers = (false, false, false);
        private (Vector2 position, bool down) _mouse = (Vector2.Zero, false);
        private float _deltaRotation = 0;
        private float _accumRotation = 0;
        private Vector2 _deltaTranslation = Vector2.Zero;

        private RectangleF _selectionBounds;
        private Matrix3x2 _selectionTransform = Matrix3x2.Identity;

        public GuideManager GuideManager { get; set; } = new GuideManager();

        public SelectionManager(
            IArtContext artView,
            IViewManager viewManager,
            IHistoryManager historyManager,
            ICacheManager cacheManager)
        {
            Context = artView;

            Selection = new ObservableList<ILayer>();
            Selection.CollectionChanged += (sender, args) =>
            {
                Update(true);
                Updated?.Invoke(this, null);
            };

            viewManager.DocumentUpdated += (sender, args) =>
            {
                if (args.PropertyName != nameof(ILayer.Selected)) return;

                var layer = (ILayer) sender;

                var contains = Selection.Contains(layer);

                if (layer.Selected && !contains)
                {
                    Selection.Add(layer);

                    foreach (var child in layer.Flatten().Skip(1))
                        child.Selected = false;
                }
                else if (!layer.Selected && contains)
                    Selection.Remove(layer);
            };

            historyManager.Traversed += (sender, args) => { Update(true); };

            cacheManager.BoundsChanged += (sender, args) => { Update(true); };
        }

        public ObservableList<ILayer> Selection { get; }

        public void SetHandle(SelectionHandle value) { _handle = value; }

        #region ISelectionManager Members

        public event EventHandler Updated;

        public void ClearSelection()
        {
            while (Selection.Count > 0) Selection[0].Selected = false;
        }

        public Vector2 FromSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectionTransform);
        }

        public void KeyDown(Key key, ModifierKeys modifiers)
        {
            if (modifiers.HasFlag(ModifierKeys.Alt))
                _modifiers = (true, _modifiers.shift, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Shift))
                _modifiers = (_modifiers.alt, true, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Control))
                _modifiers = (_modifiers.alt, _modifiers.shift, true);
        }

        public void KeyUp(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    _modifiers = (false, _modifiers.shift, _modifiers.ctrl);
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    _modifiers = (_modifiers.alt, false, _modifiers.ctrl);
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _modifiers = (_modifiers.alt, _modifiers.shift, false);
                    break;
            }
        }

        public void MouseDown(Vector2 pos)
        {
            _mouse = (pos, true);
            _deltaRotation = 0;
            _deltaTranslation = Vector2.Zero;

            #region handle testing

            SetHandle(0);

            var localPos = ToSelectionSpace(pos);

            if (Math.Abs(localPos.X - SelectionBounds.Left) < 7.5)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                    SetHandle(SelectionHandle.TopLeft);

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                    SetHandle(SelectionHandle.Left);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                    SetHandle(SelectionHandle.BottomLeft);
            }

            if (Math.Abs(localPos.X - SelectionBounds.Center.X) < 7.5)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                    SetHandle(SelectionHandle.Top);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                    SetHandle(SelectionHandle.Bottom);
            }

            if (Math.Abs(localPos.X - SelectionBounds.Right) < 7.5)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                    SetHandle(SelectionHandle.TopRight);

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                    SetHandle(SelectionHandle.Right);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                    SetHandle(SelectionHandle.BottomRight);
            }

            var vertical = FromSelectionSpace(SelectionBounds.BottomCenter) -
                           FromSelectionSpace(SelectionBounds.TopCenter);

            var rotationHandle = FromSelectionSpace(SelectionBounds.TopCenter) -
                                 Vector2.Normalize(vertical) * 15;

            if (Vector2.Distance(rotationHandle, pos) < 7.5)
                SetHandle(SelectionHandle.Rotation);

            if (_handle != 0)
                return;

            #endregion

            #region hit testing

            // for every element in the scene, perform a hit-test
            var root = Context.ViewManager.Root;

            // start by hit-testing in the existing selection, and if we find nothing,
            // then hit-test in the root
            ILayer hit = null;

            foreach (var layer in Selection)
            {
                var test = layer;

                if (_modifiers.alt)
                {
                    while (test.Parent != null)
                    {
                        var index = test.Parent.SubLayers.IndexOf(test as Layer);

                        // for every layer that is below this layer (higher index means
                        // lower z-index)
                        foreach (var sibling in test.Parent.SubLayers.Cycle(index).Skip(1)
                        )
                        {
                            hit = sibling.Hit(Context.CacheManager, pos, true);
                            if (hit != null) break;
                        }

                        test = test.Parent;
                    }
                }

                if (hit != null) break;
                hit = test.Hit(Context.CacheManager, pos, _modifiers.alt);
            }

            if (hit == null)
                hit = root.Hit(Context.CacheManager, pos, false);

            #endregion

            if (hit != null && _handle == 0)
                SetHandle(SelectionHandle.Translation);

            if (!_modifiers.shift && hit?.Selected != true)
                ClearSelection();

            if (hit != null)
                hit.Selected = true;
        }

        public void MouseMove(Vector2 pos)
        {
            var localPos = ToSelectionSpace(pos);

            #region cursor

            if (!_mouse.down)
            {
                Cursor = null;

                if (Math.Abs(localPos.X - SelectionBounds.Left) < 7.5)
                {
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                        Cursor = "cursor-resize-nwse";

                    if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                        Cursor = "cursor-resize-ew";

                    if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = "cursor-resize-nesw";
                }

                if (Math.Abs(localPos.X - SelectionBounds.Center.X) < 7.5)
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5 ||
                        Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = "cursor-resize-ns";

                if (Math.Abs(localPos.X - SelectionBounds.Right) < 7.5)
                {
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                        Cursor = "cursor-resize-nesw";

                    if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                        Cursor = "cursor-resize-ew";

                    if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = "cursor-resize-nwse";
                }

                var vertical = FromSelectionSpace(SelectionBounds.BottomCenter) -
                               FromSelectionSpace(SelectionBounds.TopCenter);

                var rotationHandle = FromSelectionSpace(SelectionBounds.TopCenter) -
                                     Vector2.Normalize(vertical) * 15;

                if (Vector2.Distance(rotationHandle, pos) < 7.5)
                    Cursor = "cursor-rotate";
            }

            #endregion

            #region transformation

            if (!_mouse.down) return;
            if (Selection.Count == 0) return;

            var relativeOrigin = new Vector2(0.5f);

            var scale = new Vector2(1);
            var translate = new Vector2(0);
            var rotate = 0f;
            var shear = 0f;

            #region rotation

            if (_handle == SelectionHandle.Rotation)
            {
                var origin = FromSelectionSpace(
                    SelectionBounds.TopLeft +
                    SelectionBounds.Size * relativeOrigin);

                rotate = MathUtils.Angle(pos - origin, false) -
                         MathUtils.Angle(_mouse.position - origin, false);

                #region segmented rotation

                if (_modifiers.shift)
                {
                    GuideManager.AddGuide(
                        new Guide(
                            0,
                            true,
                            origin,
                            _selectionTransform.GetRotation(),
                            GuideType.Rotation));

                    _accumRotation += rotate;
                    rotate = 0;

                    // setting rotate to 0 means that the transformation matrix is
                    // identity, which will cause rendering to stop so we invalidate
                    // the matrix
                    Context.InvalidateSurface();

                    if (Math.Abs(_accumRotation) > MathUtils.PiOverFour / 2)
                    {
                        var delta = Math.Sign(_accumRotation) *
                                    MathUtils.PiOverFour;

                        rotate = delta;
                        _accumRotation -= delta;
                    }
                }

                #endregion
            }

            #endregion

            #region translation

            if (_handle == SelectionHandle.Translation)
            {
                translate = pos - _mouse.position;

                #region snapped translation

                if (_modifiers.shift)
                {
                    var localCenter = SelectionBounds.TopLeft +
                                      relativeOrigin * SelectionBounds.Size;

                    var center = FromSelectionSpace(localCenter);
                    var origin = center - _deltaTranslation;

                    var localXaxis = localCenter + Vector2.UnitX;
                    var localYaxis = localCenter + Vector2.UnitY;
                    var xaxis = FromSelectionSpace(localXaxis);
                    var yaxis = FromSelectionSpace(localYaxis);

                    Vector2 axisX, axisY;

                    if (_modifiers.alt) // local axes
                    {
                        axisX = xaxis - center;
                        axisY = yaxis - center;
                    }
                    else (axisX, axisY) = (Vector2.UnitX, Vector2.UnitY);

                    GuideManager.AddGuide(
                        new Guide(
                            0,
                            true,
                            origin,
                            MathUtils.Angle(axisX, true),
                            GuideType.Position));

                    GuideManager.AddGuide(
                        new Guide(
                            1,
                            true,
                            origin,
                            MathUtils.Angle(axisY, true),
                            GuideType.Position));

                    var dest = GuideManager.LinearSnap(pos, origin, GuideType.Position)
                                           .Point;

                    translate = dest - origin - _deltaTranslation;
                }

                #endregion
            }

            #endregion

            #region scaling

            if (_handle.HasFlag(SelectionHandle.Top))
            {
                relativeOrigin.Y = 1.0f;
                scale.Y = (SelectionBounds.Bottom - localPos.Y) /
                          SelectionBounds.Height;
            }

            if (_handle.HasFlag(SelectionHandle.Left))
            {
                relativeOrigin.X = 1.0f;
                scale.X = (SelectionBounds.Right - localPos.X) /
                          SelectionBounds.Width;
            }

            if (_handle.HasFlag(SelectionHandle.Right))
            {
                relativeOrigin.X = 0.0f;
                scale.X = (localPos.X - SelectionBounds.Left) /
                          SelectionBounds.Width;
            }

            if (_handle.HasFlag(SelectionHandle.Bottom))
            {
                relativeOrigin.Y = 0.0f;
                scale.Y = (localPos.Y - SelectionBounds.Top) /
                          SelectionBounds.Height;
            }

            #region proportional scaling

            if (_modifiers.shift &&
                (_handle == SelectionHandle.BottomLeft ||
                 _handle == SelectionHandle.BottomRight ||
                 _handle == SelectionHandle.TopRight ||
                 _handle == SelectionHandle.TopLeft))
            {
                var localOrigin = SelectionBounds.TopLeft +
                                  relativeOrigin * SelectionBounds.Size;
                var localTarget = SelectionBounds.TopLeft +
                                  (Vector2.One - relativeOrigin) * SelectionBounds.Size;
                var localDest = MathUtils.Scale(localTarget, localOrigin, scale);


                var origin = FromSelectionSpace(localOrigin);
                var target = FromSelectionSpace(localTarget);
                var dest = FromSelectionSpace(localDest);

                var axis = origin - target;

                GuideManager.AddGuide(
                    new Guide(
                        0,
                        true,
                        origin,
                        MathUtils.Angle(axis, true),
                        GuideType.Proportion));

                var snap = GuideManager.LinearSnap(dest, origin, GuideType.Proportion);

                localDest = ToSelectionSpace(snap.Point);

                scale = (localDest - localOrigin) / (localTarget - localOrigin);
            }

            #endregion

            #endregion

            var size = (SelectionBounds.Size + Vector2.One) *
                       SelectionTransform.GetScale();
            var min = Vector2.One / size;

            // filter out problematic scaling values
            if (float.IsNaN(scale.X) || float.IsInfinity(scale.X)) scale.X = 1;
            if (Math.Abs(scale.X) < Math.Abs(min.X))
                scale.X = min.X * MathUtils.NonZeroSign(scale.X);

            if (float.IsNaN(scale.Y) || float.IsInfinity(scale.Y)) scale.Y = 1;
            if (Math.Abs(scale.Y) < Math.Abs(min.Y))
                scale.Y = min.Y * MathUtils.NonZeroSign(scale.Y);

            Transform(scale, translate, rotate, shear, relativeOrigin);

            _mouse = (pos, _mouse.down);

            #endregion
        }

        public void MouseUp(Vector2 pos)
        {
            _mouse = (pos, false);
            Context.InvalidateSurface();
        }

        public void Render(RenderContext target, ICacheManager cache)
        {
            foreach (var layer in Selection)
            {
                if (!(layer is IGeometricLayer shape)) continue;

                target.Transform(shape.AbsoluteTransform);

                using (var pen = target.CreatePen(1, cache.GetBrush("A1")))
                    target.DrawGeometry(Context.CacheManager.GetGeometry(shape), pen);

                target.Transform(MathUtils.Invert(shape.AbsoluteTransform));
            }

            target.Transform(SelectionTransform);

            using (var pen = target.CreatePen(1, cache.GetBrush("A1")))
                target.DrawRectangle(SelectionBounds, pen);

            target.Transform(MathUtils.Invert(SelectionTransform));

            // render guides
            foreach (var guide in GuideManager.GetGuides(GuideType.All))
            {
                 target.PushEffect(target.CreateEffect<IGlowEffect>());

                IBrush brush;

                switch (guide.Type)
                {
                    case GuideType.Proportion:
                        brush = cache.GetBrush("A1");
                        break;
                    case GuideType.Position:
                        brush = cache.GetBrush("A2");
                        break;
                    case GuideType.Rotation:
                        brush = cache.GetBrush("A3");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (guide.Type.HasFlag(GuideType.Linear))
                {
                    var origin = guide.Origin;
                    var slope = Math.Tan(guide.Angle);
                    var diagonal = target.Height / target.Width;
                    Vector2 p1, p2;

                    if (slope > diagonal)
                    {
                        p1 = new Vector2(
                            (float) (origin.X + (origin.Y - target.Height) / slope),
                            target.Height);
                        p2 = new Vector2((float) (origin.X + origin.Y / slope), 0);
                    }
                    else
                    {
                        p1 = new Vector2(
                            target.Width,
                            (float) (origin.Y + (origin.X - target.Width) * slope));
                        p2 = new Vector2(0, (float) (origin.Y + origin.X * slope));
                    }

                    using (var pen = target.CreatePen(2, brush))
                        target.DrawLine(p1, p2, pen);
                }

                if (guide.Type.HasFlag(GuideType.Radial))
                {
                    var origin = guide.Origin;
                    var axes = new[]
                    {
                        guide.Angle,
                        guide.Angle + MathUtils.PiOverFour * 1,
                        guide.Angle + MathUtils.PiOverFour * 2,
                        guide.Angle + MathUtils.PiOverFour * 3
                    };

                    using (var pen = target.CreatePen(1, brush))
                    {
                        target.DrawEllipse(origin, 20, 20, pen);

                        foreach (var x in axes)
                        target.DrawLine(origin + MathUtils.Angle(x) * 20,
                                        origin - MathUtils.Angle(x) * 20,
                                        pen);
                    }

                    using (var pen = target.CreatePen(2, brush))
                        target.DrawLine(origin - MathUtils.Angle(-axes[2]) * 25,
                                        origin,
                                        pen);

                }

                 target.PopEffect();
            } 

            GuideManager.ClearVirtualGuides();
        }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectionTransform));
        }

        public void Transform(
            Vector2 scale,
            Vector2 translate,
            float rotate,
            float shear,
            Vector2 relativeOrigin)
        {
            var localOrigin = SelectionBounds.TopLeft +
                              SelectionBounds.Size * relativeOrigin;
            var origin = FromSelectionSpace(localOrigin);

            // order doesn't really matter since only one of 
            // these will be non-default at a time

            var transform =
                MathUtils.Invert(SelectionTransform) *
                Matrix3x2.CreateScale(scale, localOrigin) *
                Matrix3x2.CreateSkew(shear, 0, localOrigin) *
                SelectionTransform *
                Matrix3x2.CreateTranslation(-origin) *
                Matrix3x2.CreateRotation(rotate) *
                Matrix3x2.CreateTranslation(translate) *
                Matrix3x2.CreateTranslation(origin);

            SelectionTransform = SelectionTransform *
                                 transform;

            _deltaRotation += rotate;
            _deltaTranslation += translate;

            if (transform.IsIdentity) return;

            var command = new TransformCommand(
                Context.HistoryManager.Position + 1,
                Selection.ToArray(),
                global: transform);

            // perform the operation
            command.Do(Context);

            // if there's already a transform command on the stack,
            // merge it with the previous one
            if (Context.HistoryManager.Current is TransformCommand current &&
                command.Time - current.Time < 500 &&
                current.Targets.SequenceEqual(command.Targets))
                Context.HistoryManager.Replace(new TransformCommand(
                                                   Context.HistoryManager.Position + 1,
                                                   Selection.ToArray(),
                                                   current.Local,
                                                   current.Global * transform));
            else
                Context.HistoryManager.Push(command);

            Context.InvalidateSurface();
        }

        public void Update(bool reset)
        {
            if (Selection.Count == 0)
            {
                SelectionBounds = RectangleF.Empty;
                SelectionTransform = Matrix3x2.Identity;
            }

            if (Selection.Count == 1)
            {
                var layer = Selection[0];

                SelectionBounds = Context.CacheManager.GetBounds(layer);
                SelectionTransform = layer.AbsoluteTransform;
            }

            if (Selection.Count > 1)
            {
                SelectionBounds = Selection.Select(Context.CacheManager.GetAbsoluteBounds)
                                           .Aggregate(RectangleF.Union);
                SelectionTransform = Matrix3x2.Identity;
            }

            Context.InvalidateSurface();
        }

        public IArtContext Context { get; }

        public string Cursor
        {
            get => _cursor;
            set
            {
                _cursor = value;
                Context.InvalidateSurface();
            }
        }

        public RectangleF SelectionBounds
        {
            get => _selectionBounds;
            private set
            {
                _selectionBounds = value;
                RaisePropertyChanged(nameof(SelectionBounds));
            }
        }

        public Matrix3x2 SelectionTransform
        {
            get => _selectionTransform;
            private set
            {
                _selectionTransform = value;
                RaisePropertyChanged(nameof(SelectionTransform));
            }
        }

        IList<ILayer> ISelectionManager.Selection => Selection;

        #endregion
    }

    public enum GuideType
    {
        All = 0,
        Linear = 1 << -1,
        Radial = 1 << -2,
        Proportion = 1 << 0 | Linear,
        Position = 1 << 1 | Linear,
        Rotation = 1 << 2 | Radial
    }
}