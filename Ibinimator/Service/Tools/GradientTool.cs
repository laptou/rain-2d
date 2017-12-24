using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public class GradientTool : SelectionToolBase<IFilledLayer>
    {
        private readonly ISet<int> _selection = new HashSet<int>();

        private int? _handle;

        //private (bool alt, bool shift) _kbd;
        private (bool down, bool moved, Vector2 pos) _mouse;

        public GradientTool(IToolManager toolManager) : base(toolManager) { Type = ToolType.Gradient; }

        public GradientBrushInfo SelectedBrush =>
            SelectedLayer?.Fill as GradientBrushInfo;
        
        public string Status =>
            "<b>Click</b> to select, " +
            "<b>Alt Click</b> to delete, " +
            "<b>Shift Click</b> to multi-select.";

        public override void ApplyFill(IBrushInfo brush)
        {
            if (brush is SolidColorBrushInfo solid)
                foreach (var handle in _selection)
                    Context.HistoryManager.Merge(
                        new ModifyGradientCommand(
                                Context.HistoryManager.Position + 1,
                                solid.Color - SelectedBrush.Stops[handle].Color,
                                new[] {handle},
                                SelectedBrush
                            ), Time.DoubleClick);
            else throw new ArgumentException(nameof(brush));
        }

        public override void ApplyStroke(IPenInfo pen)
        {
            // no-op on this tool
        }

        public override void Dispose()
        {
            _selection.Clear();
            base.Dispose();
        }

        public override void KeyDown(IArtContext context, KeyboardEvent evt)
        {
            switch ((Key) evt.KeyCode)
            {
                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    _selection.Clear();

                    break;

                case Key.Delete:
                    Remove(_selection.ToArray());
                    Context.InvalidateRender();

                    break;
            }

            base.KeyDown(context, evt);
        }

        public override void MouseDown(IArtContext context, ClickEvent evt)
        {
            var pos = evt.Position;
            _mouse = (true, false, pos);

            if (SelectedLayer == null)
            {
                base.MouseDown(context, evt);

                return;
            }

            if (SelectedBrush != null)
            {
                var t = new Func<float, Vector2>(
                    o => Vector2.Transform(
                        Vector2.Lerp(SelectedBrush.StartPoint,
                                     SelectedBrush.EndPoint, o),
                        SelectedLayer.AbsoluteTransform));

                _handle = null;
                (GradientStop stop, int index)? target = null;
                var index = 0;

                foreach (var stop in SelectedBrush.Stops)
                {
                    if (Vector2.DistanceSquared(t(stop.Offset), pos) < 16)
                    {
                        _handle = 0;
                        target = (stop, index);

                        break;
                    }

                    index++;
                }

                if (!evt.ModifierState.Shift)
                    _selection.Clear();

                if (target != null)
                {
                    _selection.Add(target.Value.index);

                    Manager.RaiseFillUpdate();
                }
            }

            Context.SelectionManager.UpdateBounds(true);
        }

        public override void MouseMove(IArtContext context, PointerEvent evt)
        {
            var pos = evt.Position;
            Context.InvalidateRender();

            if (SelectedLayer == null)
                return;

            var localLastPos = Vector2.Transform(_mouse.pos,
                                                 MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localPos = Vector2.Transform(pos,
                                             MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localDelta = localPos - localLastPos;

            _mouse = (_mouse.down, true, pos);

            if (_mouse.down && _handle != null)
            {
                switch (_handle)
                {
                    case 0:
                        var stop = SelectedBrush.Stops[_selection.First()];
                        var localStopPos =
                            Vector2.Lerp(SelectedBrush.StartPoint,
                                         SelectedBrush.EndPoint, stop.Offset) + localDelta;

                        // constrain it to the gradient axis

                        localStopPos = MathUtils.Project(
                                           localStopPos - SelectedBrush.StartPoint,
                                           SelectedBrush.EndPoint - SelectedBrush.StartPoint) +
                                       SelectedBrush.StartPoint;

                        var newOffset = Vector2.Distance(SelectedBrush.StartPoint,
                                                         localStopPos) /
                                        Vector2.Distance(
                                            SelectedBrush.StartPoint,
                                            SelectedBrush.EndPoint);

                        if (localStopPos.X < SelectedBrush.StartPoint.X)
                            newOffset = -newOffset;

                        Move(_selection.ToArray(), newOffset - stop.Offset,
                             ModifyGradientCommand.GradientOperation.ChangeOffset);

                        break;
                }

                Context.InvalidateRender();
            }
        }

        public override void MouseUp(IArtContext context, ClickEvent evt)
        {
            if (SelectedLayer == null)
                return;

            _mouse = (false, _mouse.moved, evt.Position);

            Context.InvalidateRender();
        }

        public override IBrushInfo ProvideFill()
        {
            if (SelectedBrush == null || _selection.Count == 0) return null;

            var stop = SelectedBrush.Stops[_selection.Last()];

            return new SolidColorBrushInfo(stop.Color);
        }

        public override IPenInfo ProvideStroke() { return null; }

        public override void Render(
            RenderContext target,
            ICacheManager cacheManager,
            IViewManager view)
        {
            if (SelectedBrush == null)
                return;

            var transform = SelectedLayer.AbsoluteTransform;
            var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, transform));
            var zoom = view.Zoom;

            var p = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutline)));
            var p2 = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutlineAlt)));

            Vector2 start = t(SelectedBrush.StartPoint),
                    end = t(SelectedBrush.EndPoint);

            target.DrawLine(start, end, p);

            target.FillCircle(start, 3, cacheManager.GetBrush(nameof(EditorColors.Node)));
            target.DrawCircle(start, 3, p);

            target.FillCircle(end, 3, cacheManager.GetBrush(nameof(EditorColors.Node)));
            target.DrawCircle(end, 3, p);

            var shadow = target.CreateEffect<IDropShadowEffect>();
            shadow.Color = new Color(0, 0, 0, 0.5f);
            target.PushEffect(shadow);

            for (var i = 0; i < SelectedBrush.Stops.Count; i++)
            {
                var stop = SelectedBrush.Stops[i];
                var pos = Vector2.Lerp(start, end, stop.Offset);

                target.FillCircle(pos, 4, cacheManager.GetBrush(nameof(EditorColors.Node)));

                target.DrawCircle(pos, 4, _selection.Contains(i) ? p2 : p);

                using (var brush = target.CreateBrush(stop.Color))
                {
                    target.FillCircle(pos, 2.5f, brush);
                }
            }

            target.PopEffect();
            shadow.Dispose();

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p?.Dispose();
            p2?.Dispose();
        }

        private void Move(IReadOnlyList<int> indices, float delta, ModifyGradientCommand.GradientOperation op)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(
                        Context.HistoryManager.Position + 1,
                        delta,
                        indices,
                        SelectedBrush
                    ), Time.DoubleClick);
        }

        private void Remove(params int[] indices)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(
                        Context.HistoryManager.Position + 1,
                        indices,
                        ModifyGradientCommand.GradientOperation.RemoveStop,
                        SelectedBrush
                    ), Time.DoubleClick);
        }
    }
}