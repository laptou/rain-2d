using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public class NodeTool : Model, ITool
    {
        private readonly List<(int index, bool c1, bool c2)> _selection = new List<(int, bool, bool)>();
        private bool _alt;
        private bool _down;

        private Vector2 _lastPos;
        private bool _moved;

        private bool _shift;

        public NodeTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public Shape CurrentShape => Context.SelectionManager.Selection.LastOrDefault() as Shape;

        public ToolOption[] Options => new ToolOption[0]; // TODO: add actual tool options

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;

        private Vector2 Constrain(Vector2 pos)
        {
            //var lastNode = CurrentPath.Nodes.Last();
            //var lpos = Vector2.Transform(CurrentPath.AbsoluteTransform, lastNode.Position);

            //var delta = pos - lpos;

            //if (Math.Abs(delta.Y / delta.X) > MathUtils.Sqrt3)
            //    delta = new Vector2(0, delta.Y);
            //else if (Math.Abs(delta.Y / delta.X) > MathUtils.InverseSqrt3)
            //    delta = MathUtils.Project(delta, new Vector2(1, Math.Sign(delta.Y / delta.X)));
            //else
            //    delta = new Vector2(delta.X, 0);

            //return lpos + delta;
            throw new Exception();
        }

        private void ConvertToPath()
        {
            if (!(CurrentShape is Path))
            {
                var shape = CurrentShape;
                shape.Selected = false;

                var ctp = new ConvertToPathCommand(
                    Context.HistoryManager.Position + 1,
                    new IGeometricLayer[] {shape});
                Context.HistoryManager.Do(ctp);

                ctp.Products[0].Selected = true;
            }
        }

        private void RenderGeometryHandles(RenderContext target, ICacheManager cacheManager,
            Matrix3x2 transform)
        {
            var geom = cacheManager.GetGeometry(CurrentShape);
            var nodes = geom.Read();
            var figures = nodes.Split(n => n is ClosePathInstruction);
            var index = 0;

            foreach (var figure in figures)
            {
                var figureNodes = figure.OfType<CoordinatePathInstruction>().ToArray();

                for (var i = 0; i < figureNodes.Length; i++)
                {
                    var node = figureNodes[i];

                    var prev = figureNodes[MathUtils.Wrap(i - 1, figureNodes.Length)];

                    var pos = Vector2.Transform(node.Position, transform);

                    var pen = target.CreatePen(1, cacheManager.GetBrush("A2"));

                    if (_selection.Any(n => Math.Abs(n.index - index) <= 2))
                    {
                        switch (node)
                        {
                            case CubicPathInstruction cn:
                                var control1 = Vector2.Transform(cn.Control1, transform);
                                target.DrawLine(Vector2.Transform(prev.Position, transform), control1, pen);
                                target.FillEllipse(control1, 3, 3, cacheManager.GetBrush("L0"));
                                target.DrawEllipse(control1, 3, 3, pen);

                                var control2 = Vector2.Transform(cn.Control2, transform);
                                target.DrawLine(pos, control2, pen);
                                target.FillEllipse(control2, 3, 3, cacheManager.GetBrush("L0"));
                                target.DrawEllipse(control2, 3, 3, pen);
                                break;
                            case QuadraticPathInstruction qn:
                                var control = Vector2.Transform(qn.Control, transform);

                                target.DrawLine(pos, control, pen);
                                target.FillEllipse(control, 3, 3, cacheManager.GetBrush("L0"));
                                target.DrawEllipse(control, 3, 3, pen);
                                break;
                        }
                    }

                    var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8, 8);

                    if (_selection.Any(n => n.index == index))
                        target.FillRectangle(rect,
                            rect.Contains(_lastPos) && _down
                                ? cacheManager.GetBrush("A4")
                                : cacheManager.GetBrush("A3"));
                    else if (_down)
                        target.FillRectangle(rect,
                            rect.Contains(_lastPos) ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("L1"));
                    else
                        target.FillRectangle(rect,
                            rect.Contains(_lastPos) ? cacheManager.GetBrush("A3") : cacheManager.GetBrush("L1"));

                    using (var pen2 = target.CreatePen(1,
                        i == 0 ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A2")))
                    {
                        target.DrawRectangle(rect, pen2);
                    }

                    index++;
                }

                index++;
            }
        }

        private void RenderGradientHandles(RenderContext target, ICacheManager cacheManager, Matrix3x2 transform)
        {
            if (CurrentShape.Fill is GradientBrushInfo fill)
                foreach (var stop in fill.Stops)
                {
                    var pos =
                        Vector2.Transform(
                            Vector2.Lerp(
                                fill.StartPoint,
                                fill.EndPoint,
                                stop.Offset),
                            transform);

                    using (var brush = target.CreateBrush(stop.Color))
                    {
                        target.FillEllipse(pos, 4, 4, brush);
                    }

                    using (var pen0 = target.CreatePen(2, cacheManager.GetBrush("L0")))
                    {
                        target.DrawEllipse(pos, 5, 5, pen0);
                    }

                    using (var pen2 = target.CreatePen(1, cacheManager.GetBrush("L2")))
                    {
                        target.DrawEllipse(pos, 5.5f, 5.5f, pen2);
                    }
                }
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            throw new NotImplementedException();
        }

        public void ApplyStroke(PenInfo pen)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _selection.Clear();
        }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            _shift = modifiers.HasFlag(ModifierKeys.Shift);
            _alt = modifiers.HasFlag(ModifierKeys.Alt);

            switch (key)
            {
                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    _selection.Clear();
                    break;

                case Key.Delete:
                    if (CurrentShape is Path path)
                        Context.HistoryManager.Do(
                            new ModifyPathCommand(
                                Context.HistoryManager.Position + 1,
                                path,
                                _selection.Select(n => n.index).ToArray(),
                                ModifyPathCommand.NodeOperation.Remove));

                    _selection.Clear();

                    Context.InvalidateSurface();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            _shift = modifiers.HasFlag(ModifierKeys.Shift);
            _alt = modifiers.HasFlag(ModifierKeys.Alt);

            return true;
        }

        public bool MouseDown(Vector2 pos)
        {
            _moved = false;
            _down = true;

            if (CurrentShape == null)
            {
                var hit = Root.Hit<Shape>(Context.CacheManager, pos, true);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                return false;
            }

            var tpos = Vector2.Transform(pos, MathUtils.Invert(CurrentShape.AbsoluteTransform));

            var geometry = Context.CacheManager.GetGeometry(CurrentShape);
            var instructions = geometry.Read();

            var node = instructions
                .OfType<CoordinatePathInstruction>()
                .Select((c, i) => (instruction: c, index: i))
                .FirstOrDefault(n => (n.instruction.Position - tpos).Length() < 5);

            if (node.instruction != null)
                if (_alt)
                {
                    ConvertToPath();

                    Context.HistoryManager.Do(
                        new ModifyPathCommand(
                            Context.HistoryManager.Position + 1,
                            (Path) CurrentShape,
                            new[] {node.index},
                            ModifyPathCommand.NodeOperation.Remove));

                    _selection.RemoveAll(n => n.index == node.index);
                }
                else
                {
                    if (!_shift) _selection.Clear();

                    _selection.Add((node.index, false, false));
                }

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            var tlpos = Vector2.Transform(_lastPos, MathUtils.Invert(CurrentShape.AbsoluteTransform));

            var tpos = Vector2.Transform(pos, MathUtils.Invert(CurrentShape.AbsoluteTransform));

            var delta = tpos - tlpos;

            if (_selection.Count > 0 && _down)
            {
                if (!(CurrentShape is Path))
                    ConvertToPath();

                var history = Context.HistoryManager;

                var newCmd =
                    new ModifyPathCommand(
                        history.Position + 1,
                        CurrentShape as Path,
                        _selection.Select(s => s.index).ToArray(),
                        delta,
                        ModifyPathCommand.NodeOperation.Move);

                newCmd.Do(Context);

                if (history.Current is ModifyPathCommand cmd &&
                    cmd.Operation == ModifyPathCommand.NodeOperation.Move &&
                    newCmd.Time - cmd.Time < 500 &&
                    cmd.Indices.SequenceEqual(newCmd.Indices))
                    history.Replace(
                        new ModifyPathCommand(
                            history.Position + 1,
                            cmd.Targets[0],
                            newCmd.Indices,
                            cmd.Delta + newCmd.Delta,
                            ModifyPathCommand.NodeOperation.Move));
                else
                    history.Push(newCmd);

                _lastPos = pos;

                return true;
            }

            _lastPos = pos;

            _moved = true;

            Context.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            if (!_moved && CurrentShape is Path path)
            {
                var tpos = Vector2.Transform(pos, MathUtils.Invert(path.AbsoluteTransform));

                var node = path.Instructions.OfType<CoordinatePathInstruction>().FirstOrDefault(
                    n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    var figures = path.Instructions.Split(n => n is ClosePathInstruction).ToList();
                    var index = 0;

                    foreach (var figure in figures.Select(Enumerable.ToArray))
                    {
                        var start = figure.FirstOrDefault();

                        if (start == null) continue;

                        index += figure.Length;

                        if (start != node) continue;

                        if (path.Instructions.ElementAtOrDefault(index) is ClosePathInstruction close)
                            path.Instructions[index] = new ClosePathInstruction(!close.Open);
                        else
                            Context.HistoryManager.Do(
                                new ModifyPathCommand(
                                    Context.HistoryManager.Position + 1,
                                    path,
                                    new PathInstruction[] {new ClosePathInstruction(false)},
                                    index,
                                    ModifyPathCommand.NodeOperation.Add));
                    }
                }
            }

            Context.InvalidateSurface();

            _down = false;
            return true;
        }

        public void Render(RenderContext target, ICacheManager cacheManager)
        {
            if (CurrentShape == null)
                return;

            using (var pen = target.CreatePen(1, cacheManager.GetBrush("A2")))
            {
                var transform = CurrentShape.AbsoluteTransform;

                using (var geom = cacheManager.GetGeometry(CurrentShape))
                {
                    target.Transform(transform);
                    target.DrawGeometry(geom, pen);
                    target.Transform(MathUtils.Invert(transform));
                }

                RenderGeometryHandles(target, cacheManager, transform);

                RenderGradientHandles(target, cacheManager, transform);
            }
        }

        public bool TextInput(string text)
        {
            return false;
        }

        public IBitmap Cursor => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status =>
            "<b>Click</b> to select, <b>Alt Click</b> to delete, <b>Shift Click</b> to multi-select.";

        public ToolType Type => ToolType.Node;

        #endregion
    }
}