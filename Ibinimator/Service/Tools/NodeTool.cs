﻿using System;
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
        private readonly List<int> _selection = new List<int>();
        private List<Node> _nodes = new List<Node>();

        private bool _alt;
        private bool _down;

        private Vector2 _lastPos;
        private bool _moved;
        private bool _shift;
        private Node _targetNode;

        public NodeTool(IToolManager toolManager)
        {
            Manager = toolManager;

            _nodes = GetGeometricNodes().ToList();

            Manager.Context.SelectionManager.Updated += (s, e) =>
            {
                _nodes = GetGeometricNodes().ToList();
            };
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

        private IEnumerable<Node> GetGeometricNodes()
        {
            if (CurrentShape == null) yield break;

            var geom = Context.CacheManager.GetGeometry(CurrentShape);
            var nodes = geom.Read();
            var figures = nodes.Split(n => n is ClosePathInstruction);
            var index = 0;

            foreach (var figure in figures)
            {
                var figureNodes = figure
                    .OfType<CoordinatePathInstruction>()
                    .ToArray();

                for (var i = 0; i < figureNodes.Length; i++)
                {
                    var node = new Node(index, i, figureNodes[i].Position);

                    if (figureNodes[i] is CubicPathInstruction ci)
                    {
                        var prev = new Node(index - 1,
                                            i - 1,
                                            figureNodes[i - 1].Position);

                        yield return new Node(1, i - 1, prev, ci.Control1);

                        yield return new Node(0, i, node, ci.Control2);
                    }

                    if (figureNodes[i] is QuadraticPathInstruction qi)
                        yield return new Node(0, i, node, qi.Control);

                    yield return node;

                    index++;
                }

                index++;
            }
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
                        history.Position + 1,
                        cmd.Targets[0],
                        newCmd.Indices,
                        cmd.Delta + newCmd.Delta,
                        cmd.Operation));
            else
                history.Push(newCmd);
        }

        private void Move(int[] indices, Vector2 delta)
        {
            ConvertToPath();

            var history = Context.HistoryManager;

            var newCmd =
                new ModifyPathCommand(
                    history.Position + 1,
                    CurrentShape as Path,
                    indices,
                    delta,
                    ModifyPathCommand.NodeOperation.Move);

            newCmd.Do(Context);

            _nodes = GetGeometricNodes().ToList();

            MergeCommands(newCmd);
        }

        private void Move(Node node, Vector2 delta)
        {
            ConvertToPath();

            if (node.Type == Node.NodeType.Point)
            {
                Move(new[] {node.Index}, delta);
            }
            else
            {
                var history = Context.HistoryManager;

                var operation = node.Index == 1 ?
                    ModifyPathCommand.NodeOperation.MoveHandle1 :
                    ModifyPathCommand.NodeOperation.MoveHandle2;

                var newCmd =
                    new ModifyPathCommand(
                        history.Position + 1,
                        CurrentShape as Path,
                        new[] {node.Parent.Index + node.Index},
                        delta,
                        operation);

                newCmd.Do(Context);

                _nodes = GetGeometricNodes().ToList();

                MergeCommands(newCmd);
            }
        }

        private void Remove(int index)
        {
            ConvertToPath();

            Context.HistoryManager.Do(
                new ModifyPathCommand(
                    Context.HistoryManager.Position + 1,
                    CurrentShape as Path,
                    new[] {index},
                    ModifyPathCommand.NodeOperation.Remove));

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
                    cacheManager.GetBrush("L1")
                };

            var p2 = target.CreatePen(1, a[2]);
            var p4 = target.CreatePen(1, a[4]);

            foreach (var node in _nodes)
            {
                var pos = Vector2.Transform(node.Position, transform);

                if (node.Type == Node.NodeType.Handle && _selection.Contains(node.Parent.Index))
                {
                    var parentPos = Vector2.Transform(
                        node.Parent.Position,
                        transform);

                    target.DrawLine(parentPos, pos, p2);

                    var radius = 3 / zoom;
                    var over = Vector2.DistanceSquared(pos, _lastPos) <= 9;

                    if (_down)
                        target.FillEllipse(
                            pos,
                            radius,
                            radius,
                            over ? a[2] : l[0]);
                    else
                        target.FillEllipse(
                            pos,
                            radius,
                            radius,
                            over ? a[1] : l[0]);

                    target.DrawEllipse(pos, radius, radius, p2);
                }
                else
                {
                    var radius = 4f / zoom;
                    
                    if (node.Children?.Any() == true)
                    {
                        var rect =
                            new RectangleF(pos.X - radius,
                                           pos.Y - radius,
                                           radius * 2,
                                           radius * 2);

                        var over = rect.Contains(_lastPos);

                        if (_selection.Contains(node.Index))
                            target.FillRectangle(rect, over && _down ? a[4] : a[3]);
                        else if (_down)
                            target.FillRectangle(rect, over ? a[4] : l[1]);
                        else
                            target.FillRectangle(rect, over ? a[3] : l[1]);

                        target.DrawRectangle(rect, node.FigureIndex == 0 ? p4 : p2);
                    }
                    else
                    {
                        var over = Vector2.DistanceSquared(pos, _lastPos) < radius * radius;

                        if (_selection.Contains(node.Index))
                            target.FillEllipse(pos, radius, radius, over && _down ? a[4] : a[3]);
                        else if (_down)
                            target.FillEllipse(pos, radius, radius, over ? a[4] : l[1]);
                        else
                            target.FillEllipse(pos, radius, radius, over ? a[3] : l[1]);

                        target.DrawEllipse(pos, radius, radius, node.FigureIndex == 0 ? p4 : p2);
                    }
                }
            }
            
            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p2.Dispose();
            p4.Dispose();
        }


        private void Select(int index, bool additive)
        {
            if (!additive)
                _selection.Clear();

            _selection.Add(index);
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush) { throw new NotImplementedException(); }

        public void ApplyStroke(PenInfo pen) { throw new NotImplementedException(); }

        public void Dispose() { _selection.Clear(); }

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
                    _selection.ToList().ForEach(Remove);
                    Context.InvalidateSurface();
                    break;

                case Key.K:
                    Move(_selection.ToArray(), Vector2.UnitX * 10);
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

            var tpos = Vector2.Transform(pos,
                                         MathUtils.Invert(
                                             CurrentShape
                                                 .AbsoluteTransform));

            _targetNode = GetGeometricNodes()
                .FirstOrDefault(
                    c => Vector2.Distance(c.Position, tpos) <= 5);

            if (_targetNode?.Type == Node.NodeType.Point)
                Select(_targetNode.Index, _shift);

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            var tlpos = Vector2.Transform(_lastPos,
                                          MathUtils.Invert(
                                              CurrentShape
                                                  .AbsoluteTransform));

            var tpos = Vector2.Transform(pos,
                                         MathUtils.Invert(
                                             CurrentShape
                                                 .AbsoluteTransform));

            var delta = tpos - tlpos;

            _moved = true;

            _lastPos = pos;

            if (_targetNode != null)
            {
                if (_targetNode?.Type == Node.NodeType.Handle)
                    Move(_targetNode, delta);
                else
                    Move(_selection.ToArray(), delta);

                Context.InvalidateSurface();

                return true;
            }

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (CurrentShape == null)
                return false;

            if (!_moved && _alt && _targetNode?.Type == Node.NodeType.Point)
                Remove(_targetNode.Index);

            _targetNode = null;

            if (!_moved && CurrentShape is Path path)
            {
                var tpos =
                    Vector2.Transform(pos,
                                      MathUtils.Invert(path.AbsoluteTransform));

                var node = path.Instructions
                               .OfType<CoordinatePathInstruction>()
                               .FirstOrDefault(
                                   n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    var figures = path
                        .Instructions.Split(n => n is ClosePathInstruction)
                        .ToList();
                    var index = 0;

                    foreach (var figure in figures.Select(Enumerable.ToArray))
                    {
                        var start = figure.FirstOrDefault();

                        if (start == null) continue;

                        index += figure.Length;

                        if (start != node) continue;

                        if (path.Instructions.ElementAtOrDefault(index) is
                            ClosePathInstruction close)
                            path.Instructions[index] =
                                new ClosePathInstruction(!close.Open);
                        else
                            Context.HistoryManager.Do(
                                new ModifyPathCommand(
                                    Context.HistoryManager.Position + 1,
                                    path,
                                    new PathInstruction[]
                                    {
                                        new ClosePathInstruction(false)
                                    },
                                    index,
                                    ModifyPathCommand.NodeOperation.Add));
                    }
                }
            }

            Context.InvalidateSurface();

            _down = false;
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