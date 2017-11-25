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
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class PencilTool : Core.Model.Model, ITool
    {
        private IList<PathNode> _nodes;
        private (bool down, bool moved, Vector2 pos) _mouse;
        private (bool alt, bool shift) _kbd;

        private void Remove(int index)
        {
            Context.HistoryManager.Do(
                new ModifyPathCommand(
                    Context.HistoryManager.Position + 1,
                    CurrentPath,
                    new[] { index }));

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
            if (CurrentPath == null) return Enumerable.Empty<PathNode>();

            return Context.CacheManager.GetGeometry(CurrentPath).ReadNodes();
        }

        public Path CurrentPath => Manager.Context.SelectionManager.Selection.LastOrDefault() as Path;

        public IToolOption[] Options => new IToolOption[0]; // TODO: add actual tool options

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;


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

            if (CurrentPath == null)
            {
                var hit = Root.HitTest<Path>(Context.CacheManager, pos, 0);

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

            Context.SelectionManager.Update(true);

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
            if (CurrentPath == null) return;

            var transform = CurrentPath.AbsoluteTransform;
            target.Transform(transform);

            using (var geom = cache.GetGeometry(CurrentPath))
            using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
            {
                target.DrawGeometry(geom, pen);
            }

            target.Transform(MathUtils.Invert(transform));
        }

        public bool TextInput(string text)
        {
            return false;
        }

        public string CursorImage => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion
    }
}