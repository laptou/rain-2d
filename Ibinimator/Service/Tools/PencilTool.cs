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
    public sealed class PencilTool : Model, ITool
    {
        private IList<PathNode> _nodes;
        private (bool down, bool moved, Vector2 pos) _mouse;
        private (bool alt, bool shift) _kbd;
        private Vector2? _start;

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

        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;

            _nodes = GetGeometricNodes().ToList();

            Manager.Context.SelectionManager.Updated += (s, e) => { _nodes = GetGeometricNodes().ToList(); };

            Manager.Context.HistoryManager.Traversed += (s, e) => { _nodes = GetGeometricNodes().ToList(); };
        }

        private IEnumerable<PathNode> GetGeometricNodes()
        {
            if (SelectedPath == null) return Enumerable.Empty<PathNode>();

            return Context.CacheManager.GetGeometry(SelectedPath).ReadNodes();
        }

        public Path SelectedPath => Manager.Context.SelectionManager.Selection.LastOrDefault() as Path;

        public ToolOptions Options { get; } = new ToolOptions();

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;


        #region ITool Members

        public void ApplyFill(IBrushInfo brush) { throw new NotImplementedException(); }

        public void ApplyStroke(IPenInfo pen) { throw new NotImplementedException(); }
        public IBrushInfo ProvideFill() { throw new NotImplementedException(); }
        public IPenInfo ProvideStroke() { throw new NotImplementedException(); }

        public void Dispose() { }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            _kbd.shift = modifiers.HasFlag(ModifierKeys.Shift);
            _kbd.alt = modifiers.HasFlag(ModifierKeys.Alt);

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
                var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, SelectedPath.AbsoluteTransform));
                var found = false;
                foreach (var node in _nodes)
                {
                    if (Vector2.DistanceSquared(t(node.Position), pos) < 9)
                    {
                        found = true;

                        if (_kbd.shift)
                        {
                            // shift + click = remove node
                            Remove(node.Index);
                        }
                        else
                        {
                            // click on start node = close figure
                            if (node.Index == 0 || _nodes[node.Index - 1].FigureEnd != null)
                            {
                                Context.HistoryManager.Do(
                                    new ModifyPathCommand(
                                        Context.HistoryManager.Position + 1,
                                        SelectedPath, new[] { _nodes.Count - 1 }, 
                                        ModifyPathCommand.NodeOperation.EndFigureClosed));
                            }
                        }
                    }
                    break;
                }

                // if the user didn't click on any existing nodes, create a new one
                if (!found)
                {
                    var tpos = Vector2.Transform(_mouse.pos,
                                                 MathUtils.Invert(SelectedPath.AbsoluteTransform));

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

        public bool MouseMove(Vector2 pos)
        {
            _mouse = (_mouse.down, true, pos);

            Context.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            Context.InvalidateSurface();

            return true;
        }

        public void Render(RenderContext target, ICacheManager cache, IViewManager view)
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

        public bool TextInput(string text) { return false; }

        public string Cursor => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion
    }
}