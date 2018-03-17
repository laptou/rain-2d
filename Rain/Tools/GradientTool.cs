using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Rain.Commands;
using Rain.Core;
using Rain.Core.Input;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;
using Rain.Theme;
using Rain.Utility;

using GradientOp = Rain.Commands.ModifyGradientCommand.GradientOperation;

namespace Rain.Tools
{
    public class GradientTool : SelectionToolBase<IFilledLayer>
    {
        private readonly ISet<int> _selection = new HashSet<int>();

        private (Vector2 start, Vector2 end)? _drag;

        private (bool down, bool moved, Vector2 pos) _mouse;
        private bool                                 _updatingOptions;
        private bool                                 _end, _start;

        public GradientTool(IToolManager toolManager) : base(toolManager)
        {
            Type = ToolType.Gradient;

            Options.Create<Action>("add-stop", ToolOptionType.Button, "Add Stop")
                   .SetIcon("icon-add")
                   .Set(Add);

            Options.Create<Action>("remove-stop", ToolOptionType.Button, "Remove Stop")
                   .SetIcon("icon-remove")
                   .Set(Remove);

            Options.Create<GradientBrushType>("type", ToolOptionType.Dropdown, "Type")
                   .SetValues(GradientBrushType.Linear, GradientBrushType.Radial)
                   .Set(GradientBrushType.Linear);

            Options.OptionChanged += OnOptionChanged;
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_updatingOptions) return;

            var option = (ToolOptionBase) sender;

            if (SelectedBrush == null) return;

            switch (option)
            {
                case ToolOption<GradientBrushType> gradientType when option.Id == "type":

                    SelectedBrush.Type = gradientType.Value;

                    break;
            }
        }


        /// <inheritdoc />
        protected override void OnSelectionChanged(object sender, EventArgs args)
        {
            UpdateOptions();
            base.OnSelectionChanged(sender, args);
        }

        private void UpdateOptions()
        {
            _updatingOptions = true;

            Options.Set("type", SelectedBrush?.Type ?? GradientBrushType.Linear);

            _updatingOptions = false;
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

                    break;

                case Key.OemPlus when _selection.Any() && evt.ModifierState.Shift:
                    Add();

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
                        SelectedBrush.Transform * SelectedLayer.AbsoluteTransform));

                (GradientStop stop, int index)? target = null;
                var index = 0;

                foreach (var stop in SelectedBrush.Stops)
                {
                    if (Vector2.DistanceSquared(t(stop.Offset), pos) < 8 / zoom / zoom)
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
            Context.Invalidate();

            if (SelectedLayer == null)
                return;

            var brushTransform = SelectedBrush?.Transform ?? Matrix3x2.Identity;
            var transform = MathUtils.Invert(brushTransform * SelectedLayer.AbsoluteTransform);

            var localLastPos = Vector2.Transform(_mouse.pos, transform);

            var localPos = Vector2.Transform(pos, transform);

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
                    _selection.Count == 1 &&
                    !evt.ModifierState.Alt)
                {
                    var delta = localDelta;

                    if (evt.ModifierState.Shift)
                    {
                        delta = MathUtils.Project(delta,
                                                  SelectedBrush.EndPoint -
                                                  SelectedBrush.StartPoint);
                    }

                    Move(delta,
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

                    newOffset = MathUtils.Clamp(0, 1, newOffset);

                    Move(_selection.ToArray(), newOffset - stop.Offset);
                }
            }

            Context.Invalidate();
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


            Context.Invalidate();
        }

        public override IBrushInfo ProvideFill()
        {
            if (SelectedBrush == null ||
                _selection.Count == 0) return null;

            var stop = SelectedBrush.Stops[_selection.Last()];

            return new SolidColorBrushInfo(stop.Color);
        }

        public override IPenInfo ProvideStroke() { return null; }

        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (_drag != null)
            {
                target.DrawLine(_drag.Value.start,
                                _drag.Value.end,
                                cache.GetPen(Colors.GradientHandleOutline, 1));

                return;
            }

            if (SelectedBrush == null)
                return;

            var fill = cache.GetBrush(Colors.Node);
            var fillAlt = cache.GetBrush(Colors.NodeSpecial);
            var outline = cache.GetPen(Colors.GradientHandleOutline, 1);
            var outlineSel = cache.GetPen(Colors.GradientHandleSelectedOutline, 1);
            var outlineAlt = cache.GetPen(Colors.NodeSpecialOutline, 1);

            var transform = SelectedBrush.Transform * SelectedLayer.AbsoluteTransform;
            var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, transform));
            var zoom = view.Zoom;
            var radius = 6 / zoom;

            Vector2 start = t(SelectedBrush.StartPoint), end = t(SelectedBrush.EndPoint);

            target.DrawLine(start, end, outlineAlt);

            if (SelectedBrush.Type == GradientBrushType.Linear)
            {
                target.FillRectangle(end, new Vector2(radius * 0.75f), fillAlt);
                target.DrawRectangle(end, new Vector2(radius * 0.75f), outlineAlt);
            }

            if (SelectedBrush.Type == GradientBrushType.Radial)
            {
                var s = SelectedBrush.StartPoint;
                var e = SelectedBrush.EndPoint;
                var v = t(s + new Vector2(0, e.Y - s.Y));
                var h = t(s + new Vector2(e.X - s.X, 0));

                target.DrawLine(start, v, outlineAlt);
                target.DrawLine(start, h, outlineAlt);

                target.FillRectangle(v, new Vector2(radius * 0.75f), fillAlt);
                target.DrawRectangle(v, new Vector2(radius * 0.75f), outlineAlt);

                target.FillRectangle(h, new Vector2(radius * 0.75f), fillAlt);
                target.DrawRectangle(h, new Vector2(radius * 0.75f), outlineAlt);
            }

            target.FillRectangle(start, new Vector2(radius * 0.75f), fillAlt);
            target.DrawRectangle(start, new Vector2(radius * 0.75f), outlineAlt);

            using (var fxLayer = target.CreateEffectLayer())
            {
                using (var shadow = fxLayer.CreateEffect<IDropShadowEffect>())
                {
                    shadow.Color = new Color(0, 0, 0, 0.5f);

                    fxLayer.Begin(null);
                    fxLayer.Clear(Color.Transparent);
                    fxLayer.PushEffect(shadow);

                    for (var i = 0; i < SelectedBrush.Stops.Count; i++)
                    {
                        var stop = SelectedBrush.Stops[i];
                        var pos = Vector2.Lerp(start, end, stop.Offset);

                        fxLayer.FillCircle(pos, radius * 1.25f, fill);

                        fxLayer.DrawCircle(pos,
                                           radius * 1.25f,
                                           _selection.Contains(i) ? outlineSel : outline);

                        using (var brush = fxLayer.CreateBrush(stop.Color))
                        {
                            fxLayer.FillCircle(pos, radius * 0.75f, brush);
                        }
                    }

                    fxLayer.End();

                    target.DrawEffectLayer(fxLayer);
                }
            }
        }

        private void Add(Color color, int index)
        {
            var prev = SelectedBrush.Stops[index - 1];
            var next = SelectedBrush.Stops[index];
            var stop = new GradientStop(color, MathUtils.Average(prev.Offset, next.Offset));

            var gt = _selection.Where(s => s >= index).ToArray();
            foreach (var i in gt)
                _selection.Remove(i);

            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          index,
                                          stop,
                                          SelectedBrush),
                Time.DoubleClick);

            foreach (var i in gt)
                _selection.Add(i + 1);
        }

        private void Add(int index)
        {
            var prev = SelectedBrush.Stops[index - 1];
            var next = SelectedBrush.Stops[index];
            var color = MathUtils.Average(prev.Color, next.Color);
            Add(color, index);
        }

        private void Add()
        {
            if (_selection.Count == 0) return;

            Add(Math.Max(1, _selection.First()));
        }

        private bool IsEndpoint(GradientStop stop)
        {
            if (SelectedBrush == null) return false;
            if (SelectedBrush.Stops.Count <= 2) return true;
            if (Math.Abs(stop.Offset - 1) > MathUtils.Epsilon &&
                Math.Abs(stop.Offset) > MathUtils.Epsilon) return false;
            if (Equals(stop, SelectedBrush.Stops[0])) return true;
            if (Equals(stop, SelectedBrush.Stops[SelectedBrush.Stops.Count - 1])) return true;

            return false;
        }

        private void Move(Vector2 delta, GradientOp operation)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          delta,
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

        private void Remove(IReadOnlyList<int> indices)
        {
            Context.HistoryManager.Merge(
                new ModifyGradientCommand(Context.HistoryManager.Position + 1,
                                          indices,
                                          SelectedBrush),
                Time.DoubleClick);
        }

        private void Remove() { Remove(_selection.ToArray()); }
    }
}