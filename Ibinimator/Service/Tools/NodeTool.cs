using System;
using System.Collections.Generic;
using Ibinimator.Utility;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;
using SharpDX.Direct2D1;
using Ibinimator.Renderer;
using SharpDX;
using Ellipse = SharpDX.Direct2D1.Ellipse;
using Matrix3x2 = System.Numerics.Matrix3x2;
using Rectangle = Ibinimator.Renderer.Model.Rectangle;
using RectangleF = Ibinimator.Renderer.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace Ibinimator.Service.Tools
{
    public class NodeTool : Model, ITool
    {
        private readonly List<PathNode> _selectedNodes = new List<PathNode>();
        private bool _alt;
        private bool _down;

        private (bool Center, bool RadiusX, bool RadiusY) _ellipseHandles =
            (false, false, false);

        private Vector2 _lastPos;
        private bool _moved;

        private (bool TopLeft, bool TopRight, bool BottomLeft, bool BottomRight) _rectHandles =
            (false, false, false, false);

        private bool _shift;

        public NodeTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public Shape CurrentShape => Context.SelectionManager.Selection.LastOrDefault() as Shape;

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

        private void RenderGeometryHandles(RenderContext target, ICacheManager cacheManager,
            Matrix3x2 transform)
        {
            if (CurrentShape is Path path)
            {
                var figures = path.Nodes.Split(n => n is CloseNode);

                foreach (var figure in figures)
                {
                    var nodes = figure.ToArray();

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];

                        var pos = Vector2.Transform(node.Position, transform);

                        var pen = target.CreatePen(1, cacheManager.GetBrush("A2"));

                        if (_selectedNodes.Contains(node) ||
                            _selectedNodes.Contains(nodes.ElementAtOrDefault(MathUtil.Wrap(i - 1, 0, nodes.Length))) ||
                            _selectedNodes.Contains(nodes.ElementAtOrDefault(MathUtil.Wrap(i + 1, 0, nodes.Length))))
                            switch (node)
                            {
                                case CubicPathNode cn:
                                    target.DrawEllipse(Vector2.Transform(cn.Control1, transform), 3, 3, pen);
                                    target.DrawEllipse(Vector2.Transform(cn.Control2, transform), 3, 3, pen);
                                    break;
                                case QuadraticPathNode qn:
                                    target.DrawEllipse(Vector2.Transform(qn.Control, transform), 3, 3, pen);
                                    break;
                            }

                        var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8, 8);

                        if (_selectedNodes.Contains(node))
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

                        using(var pen2 = target.CreatePen(1, i == 0 ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A2")))
                        target.DrawRectangle(rect, pen2);
                    }
                }
            }

            if (CurrentShape is Rectangle rectangle)
            {
                void RenderRectHandle(Vector2 position, bool isMouseDown)
                {
                    var p = Vector2.Transform(position, transform);
                    var r = new RectangleF(p.X - 4f, p.Y - 4f, 8, 8);

                    if (isMouseDown)
                        target.FillRectangle(r,
                            r.Contains(_lastPos) ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("L1"));
                    else
                        target.FillRectangle(r,
                            r.Contains(_lastPos) ? cacheManager.GetBrush("A3") : cacheManager.GetBrush("L1"));
                }

                RenderRectHandle(
                    new Vector2(rectangle.X, rectangle.Y), 
                    _rectHandles.TopLeft);

                RenderRectHandle(
                    new Vector2(rectangle.X + rectangle.Width, rectangle.Y), 
                    _rectHandles.TopRight);

                RenderRectHandle(
                    new Vector2(rectangle.X, rectangle.Y + rectangle.Height), 
                    _rectHandles.BottomLeft);

                RenderRectHandle(
                    new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height),
                    _rectHandles.BottomRight);
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
                        target.FillEllipse(pos, 4, 4, brush);

                    using (var pen0 = target.CreatePen(2, cacheManager.GetBrush("L0")))
                    target.DrawEllipse(pos, 5, 5, pen0);

                    using (var pen2 = target.CreatePen(1, cacheManager.GetBrush("L2")))
                    target.DrawEllipse(pos, 5.5f, 5.5f, pen2);
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
            _selectedNodes.Clear();
        }

        public bool KeyDown(Key key)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    _shift = true;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    _alt = true;
                    break;

                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    _selectedNodes.Clear();
                    break;

                case Key.Delete:
                    if (CurrentShape is Path path)
                        Context.HistoryManager.Do(
                            new ModifyPathCommand(
                                Context.HistoryManager.Position + 1,
                                path,
                                _selectedNodes.ToArray(),
                                _selectedNodes.Select(path.Nodes.IndexOf).ToArray(),
                                ModifyPathCommand.NodeOperation.Remove));

                    _selectedNodes.Clear();

                    Context.InvalidateSurface();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool KeyUp(Key key)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    _shift = false;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    _alt = false;
                    break;
                default: return false;
            }

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

            if (CurrentShape is Path path)
            {
                var node = path.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 5);

                if (node != null)
                    if (_shift)
                    {
                        _selectedNodes.Add(node);
                    }
                    else if (_alt)
                    {
                        Context.HistoryManager.Do(
                            new ModifyPathCommand(
                                Context.HistoryManager.Position + 1,
                                path,
                                new[] {node},
                                path.Nodes.IndexOf(node),
                                ModifyPathCommand.NodeOperation.Remove));

                        _selectedNodes.Remove(node);
                    }
                    else
                    {
                        _selectedNodes.Clear();
                        _selectedNodes.Add(node);
                    }
            }

            if (CurrentShape is Rectangle rect)
            {
                if (Math.Abs(tpos.X - rect.X) <= 4 && Math.Abs(tpos.Y - rect.Y) <= 4)
                    _rectHandles.TopLeft = true;

                if (Math.Abs(tpos.X - (rect.X + rect.Width)) <= 4 && Math.Abs(tpos.Y - rect.Y) <= 4)
                    _rectHandles.TopRight = true;

                if (Math.Abs(tpos.X - rect.X) <= 4 && Math.Abs(tpos.Y - (rect.Y + rect.Height)) <= 4)
                    _rectHandles.BottomLeft = true;

                if (Math.Abs(tpos.X - (rect.X + rect.Width)) <= 4 && Math.Abs(tpos.Y - (rect.Y + rect.Height)) <= 4)
                    _rectHandles.BottomRight = true;
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

            if (CurrentShape is Path path)
                if (_selectedNodes.Count > 0 && _down)
                {
                    var history = Context.HistoryManager;

                    var newCmd =
                        new ModifyPathCommand(
                            history.Position + 1,
                            path,
                            _selectedNodes.ToArray(),
                            delta,
                            ModifyPathCommand.NodeOperation.Move);

                    newCmd.Do(Context);

                    if (history.Current is ModifyPathCommand cmd &&
                        cmd.Operation == ModifyPathCommand.NodeOperation.Move &&
                        newCmd.Time - cmd.Time < 500 &&
                        cmd.Nodes.SequenceEqual(newCmd.Nodes))
                        history.Replace(
                            new ModifyPathCommand(
                                history.Position + 1,
                                path,
                                newCmd.Nodes,
                                cmd.Delta + newCmd.Delta,
                                ModifyPathCommand.NodeOperation.Move));
                    else
                        history.Push(newCmd);

                    return true;
                }

            if (CurrentShape is Rectangle rect)
            {
                if (_rectHandles.TopLeft)
                {
                    rect.X += delta.X;
                    rect.Y += delta.Y;
                }

                if (_rectHandles.TopRight)
                {
                    rect.Width += delta.X;
                    rect.Y += delta.Y;
                }

                if (_rectHandles.BottomLeft)
                {
                    rect.X += delta.X;
                    rect.Height += delta.Y;
                }

                if (_rectHandles.BottomRight)
                {
                    rect.Width += delta.X;
                    rect.Height += delta.Y;
                }
            }

            _lastPos = pos;

            _moved = true;

            Context.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            _rectHandles = (false, false, false, false);

            if (CurrentShape == null)
                return false;

            if (!_moved && CurrentShape is Path path)
            {
                var tpos = Vector2.Transform(pos, MathUtils.Invert(path.AbsoluteTransform));

                var node = path.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    var figures = path.Nodes.Split(n => n is CloseNode).ToList();
                    var index = 0;

                    foreach (var figure in figures.Select(Enumerable.ToArray))
                    {
                        var start = figure.FirstOrDefault();

                        if (start == null) continue;

                        index += figure.Length;

                        if (start != node) continue;

                        if (path.Nodes.ElementAtOrDefault(index) is CloseNode close)
                            close.Open = !close.Open;
                        else
                            Context.HistoryManager.Do(
                                new ModifyPathCommand(
                                    Context.HistoryManager.Position + 1,
                                    path,
                                    new PathNode[] {new CloseNode()},
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

        public Bitmap Cursor => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public ToolOption[] Options => new ToolOption[0]; // TODO: add actual tool options

        public string Status =>
            "<b>Click</b> to select, <b>Alt Click</b> to delete, <b>Shift Click</b> to multi-select.";

        public ToolType Type => ToolType.Node;

        #endregion
    }
}