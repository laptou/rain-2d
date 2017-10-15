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
using System.Numerics;
using Ibinimator.Renderer;

namespace Ibinimator.Service.Tools
{
    public sealed class PencilTool : Model, ITool
    {
        private bool _alt;
        private bool _down;
        private Vector2 _lastPos;
        private bool _moved;
        private bool _shift;

        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public Path CurrentPath => Manager.Context.SelectionManager.Selection.LastOrDefault() as Path;

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;

        private Vector2 Constrain(Vector2 pos)
        {
            var lastNode = CurrentPath.Nodes.Last();
            var lpos = Vector2.Transform(lastNode.Position, CurrentPath.AbsoluteTransform);

            var delta = pos - lpos;

            if (Math.Abs(delta.Y / delta.X) > MathUtils.Sqrt3)
                delta = new Vector2(0, delta.Y);
            else if (Math.Abs(delta.Y / delta.X) > MathUtils.InverseSqrt3)
                delta = MathUtils.Project(delta, new Vector2(1, Math.Sign(delta.Y / delta.X)));
            else
                delta = new Vector2(delta.X, 0);

            return lpos + delta;
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
            if (CurrentPath == null)
            {
                var hit = Root.Hit<Path>(Context.CacheManager, pos, true);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                var path = new Path
                {
                    Fill = Context.BrushManager.Fill,
                    Stroke = Context.BrushManager.Stroke
                };

                Context.HistoryManager.Do(
                    new AddLayerCommand(Context.HistoryManager.Position + 1,
                        Root,
                        path));

                path.Selected = true;
            }

            _moved = false;
            _down = true;

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            _lastPos = pos;

            _moved = true;

            Context.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (!_moved)
            {
                var tpos =
                    Vector2.Transform(pos, MathUtils.Invert(CurrentPath.AbsoluteTransform));

                var node = CurrentPath.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    if (_alt)
                        Context.HistoryManager.Do(
                            new ModifyPathCommand(
                                Context.HistoryManager.Position + 1,
                                CurrentPath,
                                new[] {node},
                                CurrentPath.Nodes.IndexOf(node),
                                ModifyPathCommand.NodeOperation.Remove));
                }
                else
                {
                    PathNode newNode;

                    if (_shift)
                    {
                        var cpos =
                            Vector2.Transform(
                                Constrain(pos), 
                                MathUtils.Invert(CurrentPath.AbsoluteTransform));

                        newNode = new PathNode {X = cpos.X, Y = cpos.Y};
                    }
                    else
                    {
                        newNode = new PathNode {X = tpos.X, Y = tpos.Y};
                    }

                    Context.HistoryManager.Do(
                        new ModifyPathCommand(
                            Context.HistoryManager.Position + 1,
                            CurrentPath,
                            new[] {newNode},
                            CurrentPath.Nodes.Count,
                            ModifyPathCommand.NodeOperation.Add));
                }
            }

            Context.InvalidateSurface();

            _down = false;
            return true;
        }

        public void Render(RenderContext target, ICacheManager cacheManager)
        {
            if (CurrentPath == null) return;
            
                var transform = CurrentPath.AbsoluteTransform;
                target.Transform(transform);

                using (var geom = cacheManager.GetGeometry(CurrentPath))
                using (var pen = target.CreatePen(1, cacheManager.GetBrush("A2")))
                        target.DrawGeometry(geom, pen);

                target.Transform(MathUtils.Invert(transform));

                var figures = CurrentPath.Nodes.Split(n => n is CloseNode);

                foreach (var figure in figures)
                {
                    var nodes = figure.ToArray();

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];

                        var pos = Vector2.Transform(node.Position, transform);
                        var zoom = Context.ViewManager.Zoom;

                        var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8 / zoom, 8 / zoom);

                        if (_down)
                            target.FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height, 
                                rect.Contains(_lastPos) ? 
                                    cacheManager.GetBrush("A4") : 
                                    cacheManager.GetBrush("L1"));
                        else
                            target.FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height,
                                rect.Contains(_lastPos) ? 
                                    cacheManager.GetBrush("A3") : 
                                    cacheManager.GetBrush("L1"));

                        using(var pen = target.CreatePen(1, i == 0 ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A2")))
                            target.DrawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, pen);
                    }
                }
        }

        public IBitmap Cursor => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public ToolOption[] Options => new ToolOption[0]; // TODO: add actual tool options

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion
    }
}