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

    public sealed class SelectionManager : Model, ISelectionManager
    {
        private IBitmap _cursor;
        private SelectionHandle _handle = 0;
        private (bool alt, bool shift, bool ctrl) _modifiers = (false, false, false);
        private (Vector2 position, bool down) _mouse = (Vector2.Zero, false);

        private RectangleF _selectionBounds;
        private Matrix3x2 _selectionTransform = Matrix3x2.Identity;

        private Guide? _scaleGuide = null;

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

                    if (layer is IContainerLayer container)
                        Selection.RemoveItems(container.SubLayers);
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

            if (!_modifiers.shift && _handle == 0)
                ClearSelection();

            if (hit != null)
                hit.Selected = true;

            #endregion

            if (hit != null && _handle == 0)
                SetHandle(SelectionHandle.Translation);
        }

        public void MouseMove(Vector2 pos)
        {
            var localPos = ToSelectionSpace(pos);
            var localPosOld = ToSelectionSpace(_mouse.position);

            #region cursor

            if (!_mouse.down)
            {
                Cursor = null;

                if (Math.Abs(localPos.X - SelectionBounds.Left) < 7.5)
                {
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-nwse");

                    if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-ew");

                    if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-nesw");
                }

                if (Math.Abs(localPos.X - SelectionBounds.Center.X) < 7.5)
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5 ||
                        Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-ns");

                if (Math.Abs(localPos.X - SelectionBounds.Right) < 7.5)
                {
                    if (Math.Abs(localPos.Y - SelectionBounds.Top) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-nesw");

                    if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-ew");

                    if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 7.5)
                        Cursor = Context.CacheManager.GetBitmap("cursor-resize-nwse");
                }

                var vertical = FromSelectionSpace(SelectionBounds.BottomCenter) -
                               FromSelectionSpace(SelectionBounds.TopCenter);

                var rotationHandle = FromSelectionSpace(SelectionBounds.TopCenter) -
                                     Vector2.Normalize(vertical) * 15;

                if (Vector2.Distance(rotationHandle, pos) < 7.5)
                    Cursor = Context.CacheManager.GetBitmap("cursor-rotate");
            }

            #endregion

            #region transformation

            if (!_mouse.down) return;

            var relativeOrigin = new Vector2(0.5f);

            var scale = new Vector2(1);
            var translate = new Vector2(0);
            var rotate = 0f;
            var shear = 0f;

            if (_handle == SelectionHandle.Rotation)
            {
                var origin = FromSelectionSpace(
                    SelectionBounds.TopLeft +
                    SelectionBounds.Size * relativeOrigin);
                rotate = MathUtils.Angle(origin - pos) -
                         MathUtils.Angle(origin - _mouse.position);
            }

            if (_handle == SelectionHandle.Translation)
                translate = pos - _mouse.position;

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

                var origin = FromSelectionSpace(localOrigin);
                var target = FromSelectionSpace(localTarget);

                var axis = target - origin;
                axis.Y = -axis.Y;

                _scaleGuide = new Guide(true,
                                        origin,
                                        MathUtils.Angle(axis));

                scale = MathUtils.Project(scale, Vector2.One);
            }
            else _scaleGuide = null;

            //if (scale.Y < 0)
            //    Debugger.Break();

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
            _scaleGuide = null;
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

            if (_scaleGuide != null)
            {
                var guide = _scaleGuide.Value;
                var origin = guide.Origin;
                var slope = Math.Tan(guide.Angle);
                Vector2 p1, p2;

                if (slope > 0.5)
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

                var dx = target as Direct2DRenderContext;
                var dc = dx?.Target.QueryInterface<SharpDX.Direct2D1.DeviceContext>();
                target.PushEffect(
                    new SharpDX.Direct2D1.Effect(dc,
                                                 SharpDX.Direct2D1.Effect.GaussianBlur));

                using (var pen = target.CreatePen(2, cache.GetBrush("A4")))
                    target.DrawLine(p1, p2, pen);

                target.PopEffect();
            }
        }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectionTransform));
        }

        public void Transform(
            Vector2 localScale,
            Vector2 localTranslate,
            float localRotate,
            float localShear,
            Vector2 relativeOrigin)
        {
            var localOrigin = SelectionBounds.TopLeft +
                              SelectionBounds.Size * relativeOrigin;
            var origin = FromSelectionSpace(localOrigin);

            // order doesn't really matter since only one of 
            // these will be non-default at a time
            var local =
                Matrix3x2.CreateTranslation(-localOrigin) *
                Matrix3x2.CreateScale(localScale) *
                Matrix3x2.CreateSkew(localShear, 0) *
                Matrix3x2.CreateTranslation(localOrigin);

            var global =
                Matrix3x2.CreateTranslation(-origin) *
                Matrix3x2.CreateRotation(localRotate) *
                Matrix3x2.CreateTranslation(localTranslate) *
                Matrix3x2.CreateTranslation(origin);

            SelectionTransform = local *
                                 SelectionTransform *
                                 global;

            if (local.IsIdentity && global.IsIdentity) return;

            var command = new TransformCommand(
                Context.HistoryManager.Position + 1,
                Selection.ToArray<ILayer>(),
                local,
                global);

            // perform the operation
            command.Do(Context);

            // if there's already a transform command on the stack,
            // merge it with the previous one
            if (Context.HistoryManager.Current is TransformCommand current &&
                command.Time - current.Time < 500 &&
                current.Targets.SequenceEqual(command.Targets))
                Context.HistoryManager.Replace(new TransformCommand(
                                                   Context.HistoryManager.Position + 1,
                                                   Selection.ToArray<ILayer>(),
                                                   current.Local * local,
                                                   current.Global * global));
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

        public IBitmap Cursor
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
        Proportion,
        Position
    }
}