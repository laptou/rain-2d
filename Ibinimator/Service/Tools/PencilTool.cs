using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class PencilTool : SelectionToolBase
    {
        private (bool down, bool moved, Vector2 pos) _mouse;
        private IList<PathNode> _nodes;
        private Vector2? _start;

        public PencilTool(IToolManager toolManager, ISelectionManager selectionManager) :
            base(toolManager, selectionManager)
        {
            Type = ToolType.Pencil;

            _nodes = GetGeometricNodes().ToList();
        }

        public Path SelectedPath => Selection.LastOrDefault() as Path;

        public string Status => "";

        private IContainerLayer Root => Context.ViewManager.Root;

        public Vector2 FromWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectedPath.AbsoluteTransform));
        }

        public override bool KeyDown(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    break;

                case Key.Delete:
                    Remove(_nodes.Count - 1);
                    Context.InvalidateSurface();
                    break;

                default:
                    return base.KeyDown(key, modifiers);
            }

            return true;
        }

        public override bool MouseDown(Vector2 pos)
        {
            _mouse = (true, false, pos);

            if (SelectedPath == null)
            {
                var hit = Root.HitTest<Path>(Context.CacheManager, pos, 0);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                if (_start == null)
                {
                    _start = pos;
                    return true;
                }

                var path = new Path
                {
                    Fill = Context.BrushManager.Fill,
                    Stroke = Context.BrushManager.Stroke,
                    Instructions =
                    {
                        new MovePathInstruction(_start.Value),
                        new LinePathInstruction(_mouse.pos)
                    }
                };

                Context.HistoryManager.Do(
                    new AddLayerCommand(Context.HistoryManager.Position + 1,
                                        Root,
                                        path));

                path.Selected = true;
                _start = null;
            }
            else
            {
                var found = false;
                foreach (var node in _nodes)
                {
                    if (Vector2.DistanceSquared(ToWorldSpace(node.Position), pos) < 9)
                    {
                        found = true;

                        if (Modifiers.shift)
                        {
                            // shift + click = remove node
                            Remove(node.Index);
                        }
                        else
                        {
                            // click on start node = close figure
                            if (node.Index == 0 || _nodes[node.Index - 1].FigureEnd != null)
                                Context.HistoryManager.Do(
                                    new ModifyPathCommand(
                                        Context.HistoryManager.Position + 1,
                                        SelectedPath, new[] {_nodes.Count - 1},
                                        ModifyPathCommand.NodeOperation.EndFigureClosed));
                        }
                    }
                    break;
                }

                // if the user didn't click on any existing nodes, create a new one
                if (!found)
                {
                    var tpos = FromWorldSpace(_mouse.pos);

                    Context.HistoryManager.Do(
                        new ModifyPathCommand(
                            Context.HistoryManager.Position + 1,
                            SelectedPath, new[] {new PathNode(_nodes.Count, tpos)},
                            _nodes.Count, ModifyPathCommand.NodeOperation.Add));
                }
            }

            _nodes = GetGeometricNodes().ToList();

            Context.SelectionManager.UpdateBounds(true);

            return true;
        }

        public override bool MouseMove(Vector2 pos)
        {
            _mouse = (_mouse.down, true, pos);

            Context.InvalidateSurface();

            return false;
        }

        public override bool MouseUp(Vector2 pos)
        {
            Context.InvalidateSurface();

            return true;
        }

        public override bool TextInput(string text) { return false; }

        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            var zoom = view.Zoom;

            var p = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.NodeOutline)));
            var p2 = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.NodeOutlineAlt)));

            var radius = 4f / zoom;

            if (_start != null)
            {
                target.DrawLine(_start.Value, _mouse.pos, p);

                var rect =
                    new RectangleF(_start.Value.X - radius,
                                   _start.Value.Y - radius,
                                   radius * 2,
                                   radius * 2);

                target.FillRectangle(rect, GetBrush(false, false));
                target.DrawRectangle(rect, p2);
            }

            if (SelectedPath == null)
            {
                p.Dispose();
                p2.Dispose();
                return;
            }

            var transform = SelectedPath.AbsoluteTransform;
            target.Transform(transform);

            using (var geom = cache.GetGeometry(SelectedPath))
            using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
            {
                target.DrawGeometry(geom, pen);
            }

            target.Transform(MathUtils.Invert(transform));

            IBrush GetBrush(bool over, bool down)
            {
                if (over)
                    if (down) return cache.GetBrush(nameof(EditorColors.NodeClick));
                    else return cache.GetBrush(nameof(EditorColors.NodeHover));

                return cache.GetBrush(nameof(EditorColors.Node));
            }


            foreach (var node in _nodes)
            {
                var pos = Vector2.Transform(node.Position, transform);

                var rect =
                    new RectangleF(pos.X - radius,
                                   pos.Y - radius,
                                   radius * 2,
                                   radius * 2);

                var over = rect.Contains(_mouse.pos);

                target.FillRectangle(rect, GetBrush(over, _mouse.down));

                target.DrawRectangle(rect, node.Index == 0 ? p2 : p);
            }

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p.Dispose();
            p2.Dispose();
        }

        public Vector2 ToWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectedPath.AbsoluteTransform);
        }

        protected override void OnSelectionUpdated(object sender, EventArgs args)
        {
            _nodes = GetGeometricNodes().ToList();
            base.OnSelectionUpdated(sender, args);
        }

        private IEnumerable<PathNode> GetGeometricNodes()
        {
            if (SelectedPath == null) return Enumerable.Empty<PathNode>();

            return Context.CacheManager.GetGeometry(SelectedPath).ReadNodes();
        }

        private void Remove(int index)
        {
            Context.HistoryManager.Do(
                new ModifyPathCommand(
                    Context.HistoryManager.Position + 1,
                    SelectedPath,
                    new[] {index},
                    ModifyPathCommand.NodeOperation.Remove));

            _nodes = GetGeometricNodes().ToList();
        }
    }
}