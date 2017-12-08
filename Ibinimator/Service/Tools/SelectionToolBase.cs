using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public abstract class SelectionToolBase : Model, ITool
    {
        private int _depth = 1;
        private (Vector2 position, bool down, long time) _mouse = (Vector2.Zero, false, 0);
        protected (bool alt, bool shift, bool ctrl) Modifiers = (false, false, false);

        protected SelectionToolBase(IToolManager manager, ISelectionManager selectionManager)
        {
            Manager = manager;
            Options = new ToolOptions();
            selectionManager.Updated += OnSelectionUpdated;
        }

        protected IArtContext Context => Manager.Context;
        protected IEnumerable<ILayer> Selection => SelectionManager.Selection;

        protected ISelectionManager SelectionManager => Context.SelectionManager;

        protected virtual void OnSelectionUpdated(object sender, EventArgs args)
        {
            _depth = Selection.Any() ? 1 : Selection.Select(l => l.Depth).Min();
        }

        protected ILayer HitTest(Vector2 pos)
        {
            // for every element in the scene, perform a hit-test
            var root = Context.ViewManager.Root;

            // start by hit-testing in the existing selection, and if we find nothing,
            // then hit-test in the root
            ILayer hit = null;

            foreach (var layer in root.Flatten(_depth).Skip(1))
            {
                var test = layer.HitTest<ILayer>(Context.CacheManager, pos, 0);

                if (test == null) continue;

                hit = test;

                if (hit.Depth < _depth) continue;

                if (Modifiers.alt && hit.Selected) continue;

                break;
            }

            return hit;
        }

        protected void RenderBoundingBox(RenderContext target, ICacheManager cache, IViewManager view)
        {
            // bounding box outlines
            target.Transform(SelectionManager.SelectionTransform);

            using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
            {
                target.DrawRectangle(SelectionManager.SelectionBounds, pen);
            }

            target.Transform(MathUtils.Invert(SelectionManager.SelectionTransform));
        }

        protected void RenderPathOutlines(RenderContext target, ICacheManager cache, IViewManager view)
        {
            foreach (var shape in Selection.OfType<IGeometricLayer>())
            {
                target.Transform(shape.AbsoluteTransform);

                using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
                {
                    target.DrawGeometry(Context.CacheManager.GetGeometry(shape), pen);
                }

                target.Transform(MathUtils.Invert(shape.AbsoluteTransform));
            }
        }

        #region ITool Members

        public virtual void ApplyFill(IBrushInfo brush)
        {
            if (!Selection.Any())
                return;

            var targets =
                Selection.SelectMany(l => l.Flatten())
                         .OfType<IFilledLayer>()
                         .ToArray();

            var command = new ApplyFillCommand(
                Context.HistoryManager.Position + 1,
                targets,
                brush,
                targets.Select(t => t.Fill).ToArray());

            Context.HistoryManager.Merge(command, 500);
        }

        public virtual void ApplyStroke(IPenInfo pen)
        {
            if (!Selection.Any())
                return;

            var targets =
                Selection.SelectMany(l => l.Flatten())
                         .OfType<IStrokedLayer>()
                         .ToArray();

            var command = new ApplyStrokeCommand(
                Context.HistoryManager.Position + 1,
                targets,
                pen,
                targets.Select(t => t.Stroke).ToArray());

            var old = Context.HistoryManager.Current;

            if (old is ApplyStrokeCommand oldStrokeCommand &&
                command.Time - old.Time <= 500)
            {
                Context.HistoryManager.Pop();

                command = new ApplyStrokeCommand(
                    command.Id,
                    command.Targets,
                    command.NewStroke,
                    oldStrokeCommand.OldStrokes);
            }

            Context.HistoryManager.Do(command);
        }

        public virtual void Dispose() { SelectionManager.Updated -= OnSelectionUpdated; }

        public virtual bool KeyDown(Key key, ModifierKeys modifiers)
        {
            if (modifiers.HasFlag(ModifierKeys.Alt))
                Modifiers = (true, Modifiers.shift, Modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Shift))
                Modifiers = (Modifiers.alt, true, Modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Control))
                Modifiers = (Modifiers.alt, Modifiers.shift, true);

            return true;
        }

        public virtual bool KeyUp(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    Modifiers = (false, Modifiers.shift, Modifiers.ctrl);
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    Modifiers = (Modifiers.alt, false, Modifiers.ctrl);
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    Modifiers = (Modifiers.alt, Modifiers.shift, false);
                    break;
            }

            return true;
        }

        public virtual bool MouseDown(Vector2 pos)
        {
            var deltaTime = Time.Now - _mouse.time;
            _mouse = (pos, true, Time.Now);

            if (deltaTime < 500)
                _depth++;

            var hit = HitTest(pos);

            if (deltaTime < 500 && hit == null)
                _depth--;

            if (!Modifiers.shift && hit?.Selected != true)
                SelectionManager.ClearSelection();

            if (hit != null)
                hit.Selected = true;

            return hit != null;
        }

        public virtual bool MouseUp(Vector2 pos)
        {
            _mouse.position = pos;
            _mouse.down = false;

            Context.InvalidateSurface();
            return false;
        }

        public virtual IBrushInfo ProvideFill()
        {
            var layer = Selection.LastOrDefault();

            if (layer is IFilledLayer filled)
                return filled.Fill;

            return null;
        }

        public virtual IPenInfo ProvideStroke()
        {
            var layer = Selection.LastOrDefault();

            if (layer is IStrokedLayer stroked)
                return stroked.Stroke;

            return null;
        }

        public abstract bool MouseMove(Vector2 pos);

        public abstract void Render(RenderContext target, ICacheManager cache, IViewManager view);

        public abstract bool TextInput(string text);

        public virtual string Cursor { get; protected set; }
        public virtual float CursorRotate { get; protected set; }
        public IToolManager Manager { get; }
        public ToolOptions Options { get; protected set; }
        public ToolType Type { get; protected set; }

        #endregion
    }
}