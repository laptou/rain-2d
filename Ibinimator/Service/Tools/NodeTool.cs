using System;
using System.Collections.Generic;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public class NodeTool : Model, ITool
    {
        private readonly ISet<int> _selection = new HashSet<int>();
        private IList<PathNode> _nodes;
        private (bool down, bool moved, Vector2 pos) _mouse;
        private (bool alt, bool shift) _kbd;
        private int? _handle;

        public NodeTool(IToolManager toolManager)
        {
            Manager = toolManager;

            UpdateNodes();

            Manager.Context.SelectionManager.Updated += OnUpdated;

            Manager.Context.HistoryManager.Traversed += OnTraversed;
        }

        private void OnTraversed(object sender, long e) { UpdateNodes(); }

        private void OnUpdated(object sender, EventArgs e) { UpdateNodes(); }

        private void UpdateNodes() { _nodes = GetGeometricNodes().ToList(); }

        public IGeometricLayer SelectedLayer =>
            Context.SelectionManager.Selection.LastOrDefault() as IGeometricLayer;

        public ToolOptions Options { get; } = new ToolOptions();
        
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

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            
        }

        public void ApplyStroke(PenInfo pen) { throw new NotImplementedException(); }

        public BrushInfo ProvideFill() { return SelectedLayer.Fill; }
        public PenInfo ProvideStroke() { return SelectedLayer.Stroke; }

        public void Dispose()
        {
            _selection.Clear();

            Manager.Context.SelectionManager.Updated -= OnUpdated;

            Manager.Context.HistoryManager.Traversed -= OnTraversed;
        }

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
                    Remove(_selection.ToArray());
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

            if (SelectedLayer == null)
            {
                var hit = Root.HitTest<IGeometricLayer>(Context.CacheManager, pos, 1);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                return false;
            }

            var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, SelectedLayer.AbsoluteTransform));

            _handle = null;
            PathNode? target = null;

            foreach (var node in _nodes)
            {
                if (Vector2.DistanceSquared(t(node.Position), pos) < 3)
                {
                    _handle = 0;
                    target = node;
                    break;
                }

                if (!_selection.Contains(node.Index)) continue;

                if (node.IncomingControl != null &&
                    Vector2.DistanceSquared(t(node.IncomingControl.Value), pos) < 2)
                {
                    _handle = -1;
                    target = node;
                    break;
                }

                if (node.OutgoingControl != null &&
                    Vector2.DistanceSquared(t(node.OutgoingControl.Value), pos) < 2)
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
            if (SelectedLayer == null)
                return false;

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

                Context.InvalidateSurface();

                return true;
            }

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (SelectedLayer == null)
                return false;

            _mouse = (false, _mouse.moved, pos);

            Context.InvalidateSurface();

            return true;
        }

        public void Render(
            RenderContext target,
            ICacheManager cacheManager,
            IViewManager view)
        {
            if (SelectedLayer == null)
                return;

            var transform = SelectedLayer.AbsoluteTransform;
            var zoom = view.Zoom;

            var p = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutline)));
            var p2 = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutlineAlt)));

            IBrush GetBrush(bool over, bool down, bool selected)
            {
                if (over)
                    if (down) return cacheManager.GetBrush(nameof(EditorColors.NodeClick));
                    else return cacheManager.GetBrush(nameof(EditorColors.NodeHover));

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
    }
}