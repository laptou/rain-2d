using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Commands;
using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Model.Text;
using Ibinimator.Core.Utility;
using Ibinimator.Resources;
using Ibinimator.Utility;

namespace Ibinimator.Tools
{
    public abstract class SelectionToolBase<T> : Core.Model.Model, ITool where T : class, ILayer
    {
        private   int                                      _depth = 1;
        private   (Vector2 position, bool down, long time) _mouse = (Vector2.Zero, false, 0);
        protected ModifierState                            State;

        protected SelectionToolBase(IToolManager manager)
        {
            Manager = manager;
            Options = new ToolOptions();
        }

        protected virtual void OnSelectionChanged(object sender, EventArgs args)
        {
            _depth = Selection.Any() ? Selection.Select(l => l.Depth).Min() : 1;
            Manager.RaiseFillUpdate();
            Manager.RaiseStrokeUpdate();
        }

        #region ITool Members

        public virtual void ApplyFill(IBrushInfo brush)
        {
            if (!Selection.Any())
                return;

            var targets = Selection.SelectMany(l => l.Flatten()).OfType<IFilledLayer>().ToArray();

            var command = new ApplyFillCommand(Context.HistoryManager.Position + 1,
                                               targets,
                                               brush,
                                               targets.Select(t => t.Fill).ToArray());

            Context.HistoryManager.Merge(command, Time.DoubleClick);
        }

        public virtual void ApplyStroke(IPenInfo pen)
        {
            if (!Selection.Any())
                return;

            var targets = Selection.SelectMany(l => l.Flatten()).OfType<IStrokedLayer>().ToArray();

            var command = new ApplyStrokeCommand(Context.HistoryManager.Position + 1,
                                                 targets,
                                                 pen,
                                                 targets.Select(t => t.Stroke).ToArray());

            Context.HistoryManager.Merge(command, Time.DoubleClick);
        }

        /// <inheritdoc />
        public virtual void Attach(IArtContext context)
        {
            context.SelectionManager.SelectionChanged += OnSelectionChanged;
            context.MouseDown += MouseDown;
            context.MouseMove += MouseMove;
            context.MouseUp += MouseUp;
            context.KeyDown += KeyDown;
            context.KeyUp += KeyUp;
        }

        /// <inheritdoc />
        public virtual void Detach(IArtContext context)
        {
            context.SelectionManager.SelectionChanged -= OnSelectionChanged;
            context.MouseDown -= MouseDown;
            context.MouseMove -= MouseMove;
            context.MouseUp -= MouseUp;
            context.KeyDown -= KeyDown;
            context.KeyUp -= KeyUp;
        }

        public virtual IBrushInfo ProvideFill()
        {
            var layer = Selection.LastOrDefault();

            if (layer is IFilled filled)
                return filled.Fill;

            return null;
        }

        public virtual IPenInfo ProvideStroke()
        {
            var layer = Selection.LastOrDefault();

            if (layer is IStroked stroked)
                return stroked.Stroke;

            return null;
        }

        public abstract void Render(RenderContext target, ICacheManager cache, IViewManager view);

        public virtual string Cursor { get; protected set; }
        public virtual float CursorRotate { get; protected set; }
        public IToolManager Manager { get; }
        public ToolOptions Options { get; protected set; }
        public ToolType Type { get; protected set; }

        #endregion

        #region Vector Transformation

        protected Vector2 FromWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectedLayer.AbsoluteTransform));
        }

        protected Vector2 ToWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectedLayer.AbsoluteTransform);
        }

        #endregion

        #region Context and Selection

        protected IArtContext Context => Manager.Context;

        protected IEnumerable<ILayer> Selection => SelectionManager.Selection;

        public T SelectedLayer => Selection.LastOrDefault() as T;

        protected ISelectionManager SelectionManager => Context.SelectionManager;

        #endregion

        #region Events

        public abstract void MouseMove(IArtContext context, PointerEvent evt);

        public virtual void KeyDown(IArtContext context, KeyboardEvent evt)
        {
            State = evt.ModifierState;
        }

        public virtual void KeyUp(IArtContext context, KeyboardEvent evt)
        {
            State = evt.ModifierState;
        }

        public virtual void MouseDown(IArtContext context, ClickEvent evt)
        {
            var deltaTime = Time.Now - _mouse.time;
            var pos = context.ViewManager.ToArtSpace(evt.Position);
            var state = evt.ModifierState;

            _mouse = (pos, true, Time.Now);

            if (deltaTime < 500)
                _depth++;

            var hit = HitTest(pos);

            if (deltaTime < 500 &&
                hit == null)
                _depth--;

            if (!state.Shift &&
                hit?.Selected != true)
                SelectionManager.ClearSelection();

            if (hit != null)
                hit.Selected = true;
        }

        public virtual void MouseUp(IArtContext context, ClickEvent evt)
        {
            _mouse.position = evt.Position;
            _mouse.down = false;

            Context.InvalidateRender();
        }

        #endregion

        #region Rendering

        protected void RenderBoundingBoxes(
            RenderContext target, ICacheManager cache, IViewManager view)
        {
            var outline =
                target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline)));
            var outlineRef = target.CreatePen(1,
                                              cache.GetBrush(
                                                  nameof(EditorColors.SelectionReferenceOutline)));

            // bounding box outlines
            target.Transform(SelectionManager.SelectionTransform);

            target.DrawRectangle(SelectionManager.SelectionBounds, outline);

            target.Transform(MathUtils.Invert(SelectionManager.SelectionTransform));

            foreach (var layer in Selection)
            {
                if (layer is Clone clone &&
                    !clone.Target.Selected)
                {
                    target.Transform(clone.Target.AbsoluteTransform);
                    target.DrawRectangle(cache.GetBounds(clone.Target), outlineRef);
                    target.Transform(MathUtils.Invert(clone.Target.AbsoluteTransform));
                }

                target.Transform(layer.AbsoluteTransform);

                target.DrawRectangle(cache.GetBounds(layer), outline);

                target.Transform(MathUtils.Invert(layer.AbsoluteTransform));
            }
        }

        protected void RenderPathOutlines(
            RenderContext target, ICacheManager cache, IViewManager view)
        {
            foreach (var shape in Selection.OfType<IGeometricLayer>())
            {
                target.Transform(shape.AbsoluteTransform);

                using (var pen =
                    target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
                {
                    target.DrawGeometry(Context.CacheManager.GetGeometry(shape), pen);
                }

                target.Transform(MathUtils.Invert(shape.AbsoluteTransform));
            }
        }

        #endregion

        #region Hit Testing

        protected virtual ILayer HitTest(ILayer layer, Vector2 position)
        {
            return layer.HitTest<ILayer>(Context.CacheManager, position, 0);
        }

        private ILayer HitTest(Vector2 position)
        {
            // for every element in the scene, perform a hit-test
            var root = Context.ViewManager.Root;

            // start by hit-testing in the existing selection, and if we find nothing,
            // then hit-test in the root
            ILayer hit = null;

            foreach (var layer in root.Flatten(_depth).Skip(1))
            {
                var test = HitTest(layer, position);

                if (test == null) continue;

                hit = test;

                if (hit.Depth < _depth) continue;

                if (State.Alt &&
                    hit.Selected) continue;

                break;
            }

            return hit;
        }

        #endregion
    }
}