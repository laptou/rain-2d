using System;
using System.Collections.Generic;
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
    public class NodeTool : Core.Model.Model, ITool
    {
        private readonly ISet<int> _selection = new HashSet<int>();
        private IList<PathNode> _nodes;
        private (bool down, bool moved, Vector2 pos) _mouse;
        private (bool alt, bool shift) _kbd;
        private int? _handle;

        public NodeTool(IToolManager toolManager)
        {
            Manager = toolManager;

            _nodes = GetGeometricNodes().ToList();

            Manager.Context.SelectionManager.Updated += (s, e) => { _nodes = GetGeometricNodes().ToList(); };

            Manager.Context.HistoryManager.Traversed += (s, e) => { _nodes = GetGeometricNodes().ToList(); };
        }

        public Shape CurrentShape =>
            Context.SelectionManager.Selection.LastOrDefault() as Shape;

        public IToolOption[] Options =>
            new IToolOption[0]; // TODO: add actual tool options

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
            if (CurrentShape is Path) return;

            var shape = CurrentShape;
            shape.Selected = false;

            var ctp = new ConvertToPathCommand(
                Context.HistoryManager.Position + 1,
                new IGeometricLayer[] {shape});
            Context.HistoryManager.Do(ctp);

            ctp.Products[0].Selected = true;
        }

        private IEnumerable<PathNode> GetGeometricNodes()
        {
            if (CurrentShape == null) return Enumerable.Empty<PathNode>();

            return Context.CacheManager.GetGeometry(CurrentShape).ReadNodes();
        }

        private void MergeCommands(ModifyPathCommand newCmd)
        {
            var history = Context.HistoryManager;

            if (history.Current is ModifyPathCommand cmd &&
                cmd.Operation == newCmd.Operation &&
                newCmd.Time - cmd.Time < 500 &&
                cmd.Indices.SequenceEqual(newCmd.Indices))
                history.Replace(
                    new ModifyPathCommand(
                        cmd.Id,
                        cmd.Targets[0],
                        cmd.Delta + newCmd.Delta,
                        newCmd.Indices,
                        cmd.Operation));
            else
                history.Push(newCmd);
        }

        private void Move(int[] indices, Vector2 delta, ModifyPathCommand.NodeOperation op)
        {
            ConvertToPath();

            var history = Context.HistoryManager;

            var newCmd =
                new ModifyPathCommand(
                    history.Position + 1,
                    CurrentShape as Path,
                    delta,
                    indices,
                    op);

            newCmd.Do(Context);

            _nodes = GetGeometricNodes().ToList();

            MergeCommands(newCmd);
        }

        private void Remove(int index)
        {
            ConvertToPath();

            Context.HistoryManager.Do(
                new ModifyPathCommand(
                    Context.HistoryManager.Position + 1,
                    CurrentShape as Path,
                    new[] {index}));

            _nodes = GetGeometricNodes().ToList();

            _selection.Remove(index);
        }

        private void RenderGeometryHandles(
            RenderContext target,
            ICacheManager cacheManager,
            Matrix3x2 transform,
            float zoom)
        {
            var a =
                new[]
                {
                    null,
                    cacheManager.GetBrush("A1"),
                    cacheManager.GetBrush("A2"),
                    cacheManager.GetBrush("A3"),
                    cacheManager.GetBrush("A4"),
                    cacheManager.GetBrush("A5")
                };

            var l =
                new[]
                {
                    cacheManager.GetBrush("L0"),
                    cacheManager.GetBrush("L1"),
                    cacheManager.GetBrush("L2")
                };

            var p2 = target.CreatePen(1, a[2]);
            var p4 = target.CreatePen(1, a[4]);

            IBrush GetBrush(bool over, bool down, bool selected)
            {
                if (over)
                    if (down) return a[4];
                    else return a[3];

                if (selected) return l[0];

                return l[2];
            }

            foreach (var node in _nodes)
            {
                var pos = Vector2.Transform(node.Position, transform);
                var radius = 6f / zoom;
                var selected = _selection.Contains(node.Index);

                if (node.IncomingControl != null || node.OutgoingControl != null)
                {
                    if (selected)
                    {
                        if (node.IncomingControl != null)
                        {
                            var lPos = Vector2.Transform(node.IncomingControl.Value, transform);
                            var lOver = Vector2.DistanceSquared(lPos, _mouse.pos) < radius * radius;

                            target.DrawLine(lPos, pos, p2);
                            target.FillCircle(lPos, radius / 1.5f, GetBrush(lOver, _mouse.down, false));
                            target.DrawCircle(lPos, radius / 1.5f, p2);
                        }

                        if (node.OutgoingControl != null)
                        {
                            var lPos = Vector2.Transform(node.OutgoingControl.Value, transform);
                            var lOver = Vector2.DistanceSquared(lPos, _mouse.pos) < radius * radius;

                            target.DrawLine(lPos, pos, p2);
                            target.FillCircle(lPos, radius / 1.5f, GetBrush(lOver, _mouse.down, false));
                            target.DrawCircle(lPos, radius / 1.5f, p2);
                        }
                    }

                    var over = Vector2.DistanceSquared(pos, _mouse.pos) < radius * radius;
                    target.FillCircle(pos, radius, GetBrush(over, _mouse.down, selected));
                    target.DrawCircle(pos, radius, node.Index == 0 ? p4 : p2);
                }
                else
                {
                    var rect =
                        new RectangleF(pos.X - radius,
                                       pos.Y - radius,
                                       radius * 2,
                                       radius * 2);

                    var over = rect.Contains(_mouse.pos);

                    target.FillRectangle(rect, GetBrush(over, _mouse.down, selected));

                    target.DrawRectangle(rect, node.Index == 0 ? p4 : p2);
                }
            }

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p2.Dispose();
            p4.Dispose();
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush) { throw new NotImplementedException(); }

        public void ApplyStroke(PenInfo pen) { throw new NotImplementedException(); }

        public void Dispose() { _selection.Clear(); }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            _kbd.shift = modifiers.HasFlag(ModifierKeys.Shift);
            _kbd.alt = modifiers.HasFlag(ModifierKeys.Alt);

            switch (key)
            {
                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    _selection.Clear();
                    break;

                case Key.Delete:
                    _selection.ToList().ForEach(Remove);
                    Context.InvalidateSurface();
                    break;

                case Key.K:
                    Move(_selection.ToArray(), Vector2.UnitX * 10, ModifyPathCommand.NodeOperation.Move);
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            _kbd.shift = modifiers.HasFlag(ModifierKeys.Shift);
            _kbd.alt = modifiers.HasFlag(ModifierKeys.Alt);

            return true;
        }

        public bool MouseDown(Vector2 pos)
        {
            _mouse = (true, false, pos);

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

            var tpos = Vector2.Transform(pos,
                                         MathUtils.Invert(CurrentShape.AbsoluteTransform));

            _handle = null;
            PathNode? target = null;

            foreach (var node in _nodes)
            {
                if (Vector2.DistanceSquared(node.Position, tpos) < 36)
                {
                    _handle = 0;
                    target = node;
                    break;
                }

                if (node.IncomingControl != null &&
                    Vector2.DistanceSquared(node.IncomingControl.Value, tpos) < 16)
                {
                    _handle = -1;
                    target = node;
                    break;
                }

                if (node.OutgoingControl != null &&
                    Vector2.DistanceSquared(node.OutgoingControl.Value, tpos) < 16)
                {
                    _handle = +1;
                    target = node;
                    break;
                }
            }

            if (target != null)
            {
                if (!_kbd.shift)
                    _selection.Clear();

                _selection.Add(target.Value.Index);
            }

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            var tlpos = Vector2.Transform(_mouse.pos,
                                          MathUtils.Invert(CurrentShape.AbsoluteTransform));

            var tpos = Vector2.Transform(pos,
                                         MathUtils.Invert(CurrentShape.AbsoluteTransform));

            var delta = tpos - tlpos;

            _mouse = (_mouse.down, true, pos);

            if (_mouse.down && _handle != null)
            {
                switch (_handle)
                {
                    case -1:
                        Move(_selection.ToArray(), delta, ModifyPathCommand.NodeOperation.MoveInHandle);
                        break;
                    case 0:
                        Move(_selection.ToArray(), delta, ModifyPathCommand.NodeOperation.Move);
                        break;
                    case +1:
                        Move(_selection.ToArray(), delta, ModifyPathCommand.NodeOperation.MoveOutHandle);
                        break;
                }

                Context.InvalidateSurface();

                return true;
            }

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            _mouse = (false, _mouse.moved, pos);

            Context.InvalidateSurface();

            return true;
        }

        public void Render(
            RenderContext target,
            ICacheManager cache,
            IViewManager view)
        {
            if (CurrentShape == null)
                return;

            RenderGeometryHandles(target,
                                  cache,
                                  CurrentShape.AbsoluteTransform,
                                  view.Zoom);
        }

        public bool TextInput(string text) { return false; }

        public string CursorImage => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status =>
            "<b>Click</b> to select, " +
            "<b>Alt Click</b> to delete, " +
            "<b>Shift Click</b> to multi-select.";

        public ToolType Type => ToolType.Node;

        #endregion

        #region Nested type: Node

        private class Node
        {
            #region NodeType enum

            public enum NodeType
            {
                Point,
                Handle
            }

            #endregion

            public Node(
                int index,
                int figureIndex,
                Vector2 position)
            {
                Index = index;
                FigureIndex = figureIndex;
                Type = NodeType.Point;
                Position = position;
            }

            public Node(
                int index,
                int figureIndex,
                Node parentNode,
                Vector2 position)
            {
                Index = index;
                FigureIndex = figureIndex;
                Type = NodeType.Handle;
                Position = position;
                Parent = parentNode;
            }

            public IReadOnlyList<Node> Children { get; }

            public int FigureIndex { get; }

            public int Index { get; }

            public Node Parent { get; }

            public Vector2 Position { get; }

            public NodeType Type { get; }
        }

        #endregion
    }
}