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
    public enum SelectionHandle
    {
        Rotation = 1 << -2,
        Translation = 1 << -1,
        Top = 1 << 0,
        Left = 1 << 1,
        Right = 1 << 2,
        Bottom = 1 << 3,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right
    }

    public sealed class SelectionManager : Model, ISelectionManager
    {
        private (bool alt, bool shift, bool ctrl) _modifiers = (false, false, false);
        private (Vector2 position, bool down) _mouse = (Vector2.Zero, false);
        private SelectionHandle _handle = 0;

        public SelectionManager(
            IArtContext artView,
            IViewManager viewManager,
            IHistoryManager historyManager,
            ICacheManager cacheManager)
        {
            Context = artView;

            Selection = new ObservableList<Layer>();
            Selection.CollectionChanged += (sender, args) =>
            {
                Update(true);
                Updated?.Invoke(this, null);
            };

            viewManager.DocumentUpdated += (sender, args) =>
            {
                if (args.PropertyName != nameof(Layer.Selected)) return;

                var layer = (Layer) sender;

                var contains = Selection.Contains(layer);

                if (layer.Selected && !contains)
                    Selection.Add(layer);
                else if (!layer.Selected && contains)
                    Selection.Remove(layer);
            };

            historyManager.Traversed += (sender, args) => { Update(true); };

            cacheManager.BoundsChanged += (sender, args) => { Update(true); };
        }

        private IBitmap _cursor;

        public IBitmap Cursor
        {
            get => _cursor;
            set
            {
                _cursor = value;
                Context.InvalidateSurface();
            }
        }

        IList<Layer> ISelectionManager.Selection => Selection;

        public RectangleF SelectionBounds { get; private set; }
        public Matrix3x2 SelectionTransform { get; private set; }
        public event EventHandler Updated;

        public void ClearSelection()
        {
            while (Selection.Count > 0) Selection[0].Selected = false;
        }

        public void MouseDown(Vector2 pos)
        {
            _mouse = (pos, true);

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
                    // alt: x-ray-select
                    // we select behind layers in the selection
                    // bubble up the selection tree
                    while (test.Parent != null)
                    {
                        var index = test.Parent.SubLayers.IndexOf(test);

                        // for every layer that is below this layer (higher index means
                        // lower z-index)
                        foreach (var sibling in test.Parent.SubLayers.Skip(index + 1))
                        {
                            hit = sibling.Hit(Context.CacheManager, pos, true);
                            if (hit != null) break;
                        }

                        test = test.Parent;
                    }
                }

                hit = test.Hit(Context.CacheManager, pos, true);
                if (hit != null) break;
            }

            if (hit == null)
                hit = root.Hit(Context.CacheManager, pos, false);

            if (hit != null)
            {
                if (!_modifiers.shift)
                {
                    // shift: multi-select
                    ClearSelection();
                }

                hit.Selected = true;
            }

            #endregion

            #region handle testing

            SetHandle(0);

            var localPos = ToSelectionSpace(pos);

            if (Math.Abs(localPos.X - SelectionBounds.Left) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10)
                    SetHandle(SelectionHandle.TopLeft);

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 10)
                    SetHandle(SelectionHandle.Left);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    SetHandle(SelectionHandle.BottomLeft);
            }

            if (Math.Abs(localPos.X - SelectionBounds.Center.X) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10)
                    SetHandle(SelectionHandle.Top);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    SetHandle(SelectionHandle.Bottom);
            }

            if (Math.Abs(localPos.X - SelectionBounds.Right) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10)
                    SetHandle(SelectionHandle.TopRight);

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 10)
                    SetHandle(SelectionHandle.Right);

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    SetHandle(SelectionHandle.BottomRight);
            }

            if (hit != null)
                SetHandle(SelectionHandle.Translation);

            #endregion
        }

        public void MouseMove(Vector2 pos)
        {
            var localPos = ToSelectionSpace(pos);
            var localPosOld = ToSelectionSpace(_mouse.position);

            #region cursor

            Cursor = null;

            if (Math.Abs(localPos.X - SelectionBounds.Left) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-nwse");

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-ew");

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-nesw");
            }

            if (Math.Abs(localPos.X - SelectionBounds.Center.X) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10 ||
                    Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-ns");
            }

            if (Math.Abs(localPos.X - SelectionBounds.Right) < 10)
            {
                if (Math.Abs(localPos.Y - SelectionBounds.Top) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-nesw");

                if (Math.Abs(localPos.Y - SelectionBounds.Center.Y) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-ew");

                if (Math.Abs(localPos.Y - SelectionBounds.Bottom) < 10)
                    Cursor = Context.CacheManager.GetBitmap("cursor-resize-nwse");
            }

            #endregion

            #region transformation

            if (!_mouse.down) return;

            var origin = new Vector2(0.5f);
            var scale = new Vector2(1);
            var translate = new Vector2(0);
            var rotate = 0f;
            var shear = 0f;

            if (_handle == SelectionHandle.Rotation)
                rotate = MathUtils.Angle(pos) - MathUtils.Angle(_mouse.position);

            if (_handle == SelectionHandle.Translation)
                translate = pos - _mouse.position;

            if (_handle.HasFlag(SelectionHandle.Top))
            {
                origin.Y = 1.0f;
                scale.Y = (SelectionBounds.Bottom - localPos.Y) /
                          MathUtils.AbsMax(1, SelectionBounds.Bottom - localPosOld.Y);
            }

            if (_handle.HasFlag(SelectionHandle.Left))
            {
                origin.X = 1.0f;
                scale.X = (SelectionBounds.Right - localPos.X) /
                          MathUtils.AbsMax(1, SelectionBounds.Right - localPosOld.X);
            }

            if (_handle.HasFlag(SelectionHandle.Right))
            {
                origin.X = 0.0f;
                scale.X = (localPos.X - SelectionBounds.Left) /
                          MathUtils.AbsMax(1, localPosOld.X - SelectionBounds.Left);
            }

            if (_handle.HasFlag(SelectionHandle.Bottom))
            {
                origin.Y = 0.0f;
                scale.Y = (localPos.Y - SelectionBounds.Top) /
                          MathUtils.AbsMax(1, localPosOld.Y - SelectionBounds.Top);
            }

            // filter out problematic scaling values
            if (float.IsNaN(scale.X) || float.IsInfinity(scale.X)) scale.X = 1;
            if (Math.Abs(scale.X) < MathUtils.Epsilon)
                scale.X = MathUtils.Epsilon * Math.Sign(scale.X);

            if (float.IsNaN(scale.Y) || float.IsInfinity(scale.Y)) scale.Y = 1;
            if (Math.Abs(scale.Y) < MathUtils.Epsilon)
                scale.Y = MathUtils.Epsilon * Math.Sign(scale.Y);

            Transform(scale, translate, rotate, shear, origin);

            _mouse = (pos, _mouse.down);

            #endregion
        }

        public void MouseUp(Vector2 pos) { _mouse = (pos, false); }

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
            if (modifiers.HasFlag(ModifierKeys.Alt))
                _modifiers = (false, _modifiers.shift, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Shift))
                _modifiers = (_modifiers.alt, false, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Control))
                _modifiers = (_modifiers.alt, _modifiers.shift, false);
        }

        public void Render(RenderContext target, ICacheManager cache)
        {
            foreach (var layer in Selection)
            {
                if (layer is IGeometricLayer shape)
                {
                    target.Transform(shape.AbsoluteTransform);

                    using (var pen =
                        target.CreatePen(1, cache.GetBrush("A1")))
                    {
                        target.DrawGeometry(
                            Context.CacheManager
                                   .GetGeometry(shape),
                            pen);
                    }

                    target.Transform(
                        MathUtils.Invert(shape.AbsoluteTransform));
                }
            }

            target.Transform(SelectionTransform);

            using (var pen = target.CreatePen(1, cache.GetBrush("A1")))
            {
                target.DrawRectangle(SelectionBounds, pen);
            }

            target.Transform(MathUtils.Invert(SelectionTransform));
        }

        public Vector2 FromSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectionTransform);
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
                              new Vector2(
                                  SelectionBounds.Width * relativeOrigin.X,
                                  SelectionBounds.Height * relativeOrigin.Y);
            var origin = FromSelectionSpace(localOrigin);

            // order doesn't really matter since only one of 
            // these will be non-default at a time
            var transform =
                Matrix3x2.CreateTranslation(-origin) *
                Matrix3x2.CreateScale(scale) *
                Matrix3x2.CreateTranslation(translate) *
                Matrix3x2.CreateRotation(rotate) *
                Matrix3x2.CreateSkew(shear, 0) *
                Matrix3x2.CreateTranslation(origin);

            SelectionTransform = SelectionTransform * transform;

            if (transform.IsIdentity) return;

            var command = new TransformCommand(
                Context.HistoryManager.Position + 1,
                Selection.ToArray<ILayer>(),
                transform);

            // perform the operation
            command.Do(Context);

            // if there's already a transform command on the stack,
            // merge it with the previous one
            if (Context.HistoryManager.Current is TransformCommand current &&
                current.Time - command.Time < 500 &&
                Enumerable.SequenceEqual(current.Targets, command.Targets))
            {
                Context.HistoryManager.Replace(new TransformCommand(
                                                   Context.HistoryManager.Position + 1,
                                                   Selection.ToArray<ILayer>(),
                                                   transform * current.Transform));
            }
            else
            {
                Context.HistoryManager.Push(command);
            }

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

        public ObservableList<Layer> Selection { get; }

        public IArtContext Context { get; }

        public void SetHandle(SelectionHandle value) { _handle = value; }
    }
}