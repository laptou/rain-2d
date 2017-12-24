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
    public class NodeTool : SelectionToolBase<IGeometricLayer>
    {
        private readonly ISet<int> _selection = new HashSet<int>();

        private int? _handle;

        //private (bool alt, bool shift) _kbd;
        private (bool down, bool moved, Vector2 pos) _mouse;
        private IList<PathNode>                      _nodes;

        public NodeTool(IToolManager toolManager)
            : base(toolManager)
        {
            Type = ToolType.Node;

            UpdateNodes();
        }

        public string Status =>
            "<b>Click</b> to select, " +
            "<b>Alt Click</b> to delete, " +
            "<b>Shift Click</b> to multi-select.";

        /// <inheritdoc />
        public override void Attach(IArtContext context)
        {
            Manager.Context.HistoryManager.Traversed += OnTraversed;
            base.Attach(context);
        }

        /// <inheritdoc />
        public override void Detach(IArtContext context)
        {
            Manager.Context.HistoryManager.Traversed += OnTraversed;
            base.Detach(context);
        }

        public override void Dispose()
        {
            _selection.Clear();

            Manager.Context.SelectionManager.SelectionUpdated -= OnSelectionUpdated;
            Manager.Context.HistoryManager.Traversed -= OnTraversed;

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

                case Key.Right:
                    Move(_selection.ToArray(), Vector2.UnitX * 10, ModifyPathCommand.NodeOperation.Move);

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

            _handle = null;
            PathNode? target = null;

            foreach (var node in _nodes)
            {
                if (Vector2.DistanceSquared(SelectionManager.FromSelectionSpace(node.Position), pos) < 3)
                {
                    _handle = 0;
                    target = node;

                    break;
                }

                if (!_selection.Contains(node.Index)) continue;

                if (node.IncomingControl != null &&
                    Vector2.DistanceSquared(
                        SelectionManager.FromSelectionSpace(node.IncomingControl.Value), pos) < 2)
                {
                    _handle = -1;
                    target = node;

                    break;
                }

                if (node.OutgoingControl != null &&
                    Vector2.DistanceSquared(
                        SelectionManager.FromSelectionSpace(node.OutgoingControl.Value), pos) < 2)
                {
                    _handle = +1;
                    target = node;

                    break;
                }
            }

            if (target != null)
            {
                if (!evt.ModifierState.Shift)
                    _selection.Clear();

                _selection.Add(target.Value.Index);
            }

            Context.SelectionManager.UpdateBounds(true);
        }

        public override void MouseMove(IArtContext context, PointerEvent evt)
        {
            if (SelectedLayer == null)
                return;

            var pos = evt.Position;

            var tlpos = Vector2.Transform(_mouse.pos,
                                          MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var tpos = Vector2.Transform(pos,
                                         MathUtils.Invert(SelectedLayer.AbsoluteTransform));

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

                Context.InvalidateRender();
            }
        }

        public override void MouseUp(IArtContext context, ClickEvent evt)
        {
            if (SelectedLayer == null)
            {
                base.MouseUp(context, evt);

                return;
            }

            _mouse = (false, _mouse.moved, evt.Position);

            Context.InvalidateRender();
        }

        public override void Render(
            RenderContext target,
            ICacheManager cacheManager,
            IViewManager view)
        {
            if (SelectedLayer == null)
                return;

            RenderBoundingBox(target, cacheManager, view);
            RenderPathOutlines(target, cacheManager, view);

            var transform = SelectedLayer.AbsoluteTransform;
            var zoom = view.Zoom;

            var p = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutline)));
            var p2 = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutlineAlt)));

            IBrush GetBrush(bool over, bool down, bool selected)
            {
                if (over)
                    if (down)
                        return cacheManager.GetBrush(nameof(EditorColors.NodeClick));
                    else
                        return cacheManager.GetBrush(nameof(EditorColors.NodeHover));

                if (selected) return cacheManager.GetBrush(nameof(EditorColors.NodeSelected));

                return cacheManager.GetBrush(nameof(EditorColors.Node));
            }

            var start = true;

            foreach (var node in _nodes)
            {
                var pos = Vector2.Transform(node.Position, transform);
                var radius = 4f / zoom;
                var selected = _selection.Contains(node.Index);

                if (node.IncomingControl != null || node.OutgoingControl != null)
                {
                    if (selected)
                    {
                        if (node.IncomingControl != null)
                        {
                            var lPos = Vector2.Transform(node.IncomingControl.Value, transform);
                            var lOver = Vector2.DistanceSquared(lPos, _mouse.pos) < radius * radius;

                            target.DrawLine(lPos, pos, p);
                            target.FillCircle(lPos, radius / 1.2f, GetBrush(lOver, _mouse.down, false));
                            target.DrawCircle(lPos, radius / 1.2f, p);
                        }

                        if (node.OutgoingControl != null)
                        {
                            var lPos = Vector2.Transform(node.OutgoingControl.Value, transform);
                            var lOver = Vector2.DistanceSquared(lPos, _mouse.pos) < radius * radius;

                            target.DrawLine(lPos, pos, p);
                            target.FillCircle(lPos, radius / 1.2f, GetBrush(lOver, _mouse.down, false));
                            target.DrawCircle(lPos, radius / 1.2f, p);
                        }
                    }

                    var over = Vector2.DistanceSquared(pos, _mouse.pos) < radius * radius;
                    target.FillCircle(pos, radius, GetBrush(over, _mouse.down, selected));
                    target.DrawCircle(pos, radius, node.Index == 0 ? p2 : p);
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

                    target.DrawRectangle(rect, start ? p2 : p);
                }

                start = node.FigureEnd != null;
            }

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p.Dispose();
            p2.Dispose();
        }

        protected override ILayer HitTest(ILayer layer, Vector2 position)
        {
            return layer.HitTest<IGeometricLayer>(Context.CacheManager, position, 0);
        }

        protected override void OnSelectionUpdated(object sender, EventArgs e)
        {
            UpdateNodes();
            base.OnSelectionUpdated(sender, e);
        }

        private void ConvertToPath()
        {
            if (SelectedLayer is Path) return;

            var shape = SelectedLayer;
            shape.Selected = false;

            var ctp = new ConvertToPathCommand(
                Context.HistoryManager.Position + 1,
                new[] {shape});
            Context.HistoryManager.Do(ctp);

            ctp.Products[0].Selected = true;
        }

        private IEnumerable<PathNode> GetGeometricNodes()
        {
            if (SelectedLayer == null) return Enumerable.Empty<PathNode>();

            return Context.CacheManager.GetGeometry(SelectedLayer).ReadNodes();
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
                    SelectedLayer as Path,
                    delta,
                    indices,
                    op);

            newCmd.Do(Context);

            UpdateNodes();

            MergeCommands(newCmd);
        }

        private void OnTraversed(object sender, long e) { UpdateNodes(); }

        private void Remove(params int[] indices)
        {
            ConvertToPath();

            Context.HistoryManager.Do(
                new ModifyPathCommand(
                    Context.HistoryManager.Position + 1,
                    SelectedLayer as Path,
                    indices,
                    ModifyPathCommand.NodeOperation.Remove));

            UpdateNodes();

            foreach (var index in indices)
                _selection.Remove(index);
        }

        private void UpdateNodes() { _nodes = GetGeometricNodes().ToList(); }
    }
}