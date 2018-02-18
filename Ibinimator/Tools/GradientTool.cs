using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Commands;
using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Effects;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Utility;
using Ibinimator.Resources;
using Ibinimator.Utility;

using GradientOp = Ibinimator.Commands.ModifyGradientCommand.GradientOperation;

namespace Ibinimator.Tools
{
    public class GradientTool : SelectionToolBase<IFilledLayer>
    {
        private readonly ISet<int> _selection = new HashSet<int>();

        private (Vector2 start, Vector2 end)? _drag;

        private (bool down, bool moved, Vector2 pos) _mouse;

        public GradientTool(IToolManager toolManager) : base(toolManager)
        {
            Type = ToolType.Gradient;
        }

        public GradientBrushInfo SelectedBrush =>
            SelectedLayer?.Fill as GradientBrushInfo;

        public string Status =>
            "<b>Click</b> to select, " + "<b>Alt Click</b> to delete, " +
            "<b>Shift Click</b> to multi-select.";

        public override void ApplyFill(IBrushInfo brush)
        {
            if (brush is SolidColorBrushInfo solid)
                foreach (var handle in _selection)
                    Context.HistoryManager.Merge(
                        new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                                  solid.Color - SelectedBrush
                                                               .Stops[handle]
                                                               .Color,
                                                  new[] {handle},
                                                  SelectedBrush),
                        Time.DoubleClick);
            else throw new ArgumentException(nameof(brush));
        }

        public override void ApplyStroke(IPenInfo pen)
        {
            // no-op on this tool
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
            var pos = context.ViewManager.ToArtSpace(evt.Position);
            _mouse = (true, false, pos);

            if (SelectedLayer == null)
            {
                base.MouseDown(context, evt);

                return;
            }

            if (SelectedBrush != null)
            {
                var zoom = Context.ViewManager.Zoom;
                var t = new Func<float, Vector2>(
                    o => Vector2.Transform(
                        Vector2.Lerp(SelectedBrush.StartPoint, SelectedBrush.EndPoint, o),
                        SelectedLayer.AbsoluteTransform));

                (GradientStop stop, int index)? target = null;
                var index = 0;

                foreach (var stop in SelectedBrush.Stops)
                {
                    if (Vector2.DistanceSquared(t(stop.Offset), pos) < 6 / zoom / zoom)
                    {
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
            else
            {
                _drag = (pos, pos);
            }

            Context.SelectionManager.UpdateBounds();
        }

        public override void MouseMove(IArtContext context, PointerEvent evt)
        {
            var pos = context.ViewManager.ToArtSpace(evt.Position);
            Context.InvalidateRender();

            if (SelectedLayer == null)
                return;

            var localLastPos =
                Vector2.Transform(_mouse.pos, MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localPos =
                Vector2.Transform(pos, MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localDelta = localPos - localLastPos;

            _mouse = (_mouse.down, true, pos);

            if (!_mouse.down) return;

            if (SelectedBrush != null &&
                _selection.Any())
            {
                var stop = SelectedBrush.Stops[_selection.First()];
                var localStopPos =
                    Vector2.Lerp(SelectedBrush.StartPoint, SelectedBrush.EndPoint, stop.Offset) +
                    localDelta;

                if (IsEndpoint(stop) &&
                    _selection.Count == 1)
                {
                    Move(_selection.ToArray(),
                         localDelta,
                         Equals(stop, SelectedBrush.Stops[0])
                             ? GradientOp.ChangeStart
                             : GradientOp.ChangeEnd);
                }
                else
                {
                    // constrain it to the gradient axis

                    localStopPos =
                        MathUtils.Project(localStopPos - SelectedBrush.StartPoint,
                                          SelectedBrush.EndPoint - SelectedBrush.StartPoint) +
                        SelectedBrush.StartPoint;

                    var newOffset = Vector2.Distance(SelectedBrush.StartPoint, localStopPos) /
                                    Vector2.Distance(SelectedBrush.StartPoint,
                                                     SelectedBrush.EndPoint);

                    if (localStopPos.X < SelectedBrush.StartPoint.X)
                        newOffset = -newOffset;

                    Move(_selection.ToArray(), newOffset - stop.Offset);
                }
            }

            Context.InvalidateRender();
            if (_drag != null)
                _drag = (_drag.Value.start, pos);
        }

        public override void MouseUp(IArtContext context, ClickEvent evt)
        {
            if (SelectedLayer == null)
                return;

            _mouse = (false, _mouse.moved, evt.Position);

            if (_drag != null &&
                SelectedLayer != null)
            {
                var lastBrush = Context.BrushManager.BrushHistory.LastOrDefault();

                GradientBrushInfo gradient = null;

                if (lastBrush is GradientBrushInfo lastGradient)
                    gradient = lastGradient.Clone<GradientBrushInfo>();
                else if (lastBrush is SolidColorBrushInfo lastColor)
                    gradient = new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(new[]
                        {
                            new GradientStop(lastColor.Color, 0),
                            new GradientStop(lastColor.Color, 1)
                        })
                    };

                if (gradient == null) return;

                gradient.StartPoint = FromWorldSpace(_drag.Value.start);
                gradient.EndPoint = FromWorldSpace(_drag.Value.end);

                SelectedLayer.Fill = gradient;

                _drag = null;
            }


            Context.InvalidateRender();
        }

        public override IBrushInfo ProvideFill()
        {
            if (SelectedBrush == null ||
                _selection.Count == 0) return null;

            var stop = SelectedBrush.Stops[_selection.Last()];

            return new SolidColorBrushInfo(stop.Color);
        }

        public override IPenInfo ProvideStroke() { return null; }

        public override void Render(
            RenderContext target, ICacheManager cacheManager, IViewManager view)
        {
            var fill = cacheManager.GetBrush(nameof(EditorColors.Node));
            var outline = cacheManager.GetBrush(nameof(EditorColors.NodeOutline));
            var outlineAlt = cacheManager.GetBrush(nameof(EditorColors.NodeOutlineAlt));

            if (_drag != null)
                using (var n = target.CreatePen(1, outline))
                {
                    target.DrawLine(_drag.Value.start, _drag.Value.end, n);
                }

            if (SelectedBrush == null)
                return;

            var transform = SelectedLayer.AbsoluteTransform;
            var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, transform));
            var zoom = view.Zoom;
            var radius = 6 / zoom;

            var p = target.CreatePen(1, outline);
            var p2 = target.CreatePen(1, outlineAlt);

            Vector2 start = t(SelectedBrush.StartPoint), end = t(SelectedBrush.EndPoint);

            target.DrawLine(start, end, p);

            target.FillCircle(start, radius, fill);
            target.DrawCircle(start, radius, p);

            target.FillCircle(end, radius, fill);
            target.DrawCircle(end, radius, p);

            var shadow = target.CreateEffect<IDropShadowEffect>();
            shadow.Color = new Color(0, 0, 0, 0.5f);
            target.PushEffect(shadow);

            for (var i = 0; i < SelectedBrush.Stops.Count; i++)
            {
                var stop = SelectedBrush.Stops[i];
                var pos = Vector2.Lerp(start, end, stop.Offset);

                target.FillCircle(pos, radius * 1.25f, fill);

                target.DrawCircle(pos, radius * 1.25f, _selection.Contains(i) ? p2 : p);

                using (var brush = target.CreateBrush(stop.Color))
                {
                    target.FillCircle(pos, radius * 0.75f, brush);
                }
            }

            target.PopEffect();
            shadow.Dispose();

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p?.Dispose();
            p2?.Dispose();
        }

        private bool IsEndpoint(GradientStop stop)
        {
            if (SelectedBrush == null) return false;
            if (SelectedBrush.Stops.Count <= 2) return true;
            if (Equals(stop, SelectedBrush.Stops[0])) return true;
            if (Equals(stop, SelectedBrush.Stops[SelectedBrush.Stops.Count - 1])) return true;

            return false;
        }

        private void Move(IReadOnlyList<int> indices, Vector2 delta, GradientOp operation)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          delta,
                                          indices,
                                          operation,
                                          SelectedBrush),
                Time.DoubleClick);
        }

        private void Move(IReadOnlyList<int> indices, float delta)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          delta,
                                          indices,
                                          SelectedBrush),
                Time.DoubleClick);
        }

        private void Remove(params int[] indices)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          indices,
                                          GradientOp.RemoveStop,
                                          SelectedBrush),
                Time.DoubleClick);
        }
    }
}