using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public class GradientTool : Core.Model.Model, ITool
    {
        private readonly ISet<int> _selection = new HashSet<int>();
        private (bool down, bool moved, Vector2 pos) _mouse;
        private (bool alt, bool shift) _kbd;
        private int? _handle;

        public GradientTool(IToolManager toolManager)
        {
            Manager = toolManager;

//            Manager.Context.SelectionManager.Updated += OnUpdated;
//
//            Manager.Context.HistoryManager.Traversed += OnTraversed;
        }

        public IFilledLayer SelectedLayer =>
            Context.SelectionManager.Selection.LastOrDefault() as IFilledLayer;

        public GradientBrushInfo SelectedBrush =>
            SelectedLayer?.Fill as GradientBrushInfo;

        public ToolOptions Options { get; } = new ToolOptions();

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;

        private void Move(int[] indices, float delta, ModifyGradientCommand.GradientOperation op)
        {
            throw new NotImplementedException();
        }

        private void Remove(params int[] indices)
        {
            throw new NotImplementedException();
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush) { throw new NotImplementedException(); }

        public void ApplyStroke(PenInfo pen) { throw new NotImplementedException(); }

        public BrushInfo ProvideFill()
        {
            if (SelectedBrush == null || _selection.Count == 0) return null;

            var stop = SelectedBrush.Stops[_selection.Last()];
            return new SolidColorBrushInfo(stop.Color);
        }

        public PenInfo ProvideStroke() { return null; }

        public void Dispose()
        {
            _selection.Clear();

//            Manager.Context.SelectionManager.Updated -= OnUpdated;
//
//            Manager.Context.HistoryManager.Traversed -= OnTraversed;
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
                var hit = Root.HitTest<IFilledLayer>(Context.CacheManager, pos, 1);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                return false;
            }

            if (SelectedBrush != null)
            {
                var t = new Func<float, Vector2>(
                    o => Vector2.Transform(
                        Vector2.Lerp(SelectedBrush.StartPoint,
                                     SelectedBrush.EndPoint, o),
                        SelectedLayer.AbsoluteTransform));

                _handle = null;
                (GradientStop stop, int index)? target = null;
                var index = 0;

                foreach (var stop in SelectedBrush.Stops)
                {
                    if (Vector2.DistanceSquared(t(stop.Offset), pos) < 3)
                    {
                        _handle = 0;
                        target = (stop, index);
                        break;
                    }

                    index++;
                }


                if (target != null)
                {
                    if (!_kbd.shift)
                        _selection.Clear();

                    _selection.Add(target.Value.index);
                }
            }

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            Context.InvalidateSurface();

            if (SelectedLayer == null)
                return false;

            var localLastPos = Vector2.Transform(_mouse.pos,
                                                 MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localPos = Vector2.Transform(pos,
                                             MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            var localDelta = localPos - localLastPos;

            _mouse = (_mouse.down, true, pos);

            if (_mouse.down && _handle != null)
            {
                switch (_handle)
                {
                    case 0:
                        var stop = SelectedBrush.Stops[_selection.First()];
                        var localStopPos =
                            Vector2.Lerp(SelectedBrush.StartPoint,
                                         SelectedBrush.EndPoint, stop.Offset) + localDelta;

                        // constrain it to the gradient axis

                        localStopPos = MathUtils.Project(
                                           localStopPos - SelectedBrush.StartPoint,
                                           SelectedBrush.EndPoint - SelectedBrush.StartPoint) +
                                       SelectedBrush.StartPoint;

                        var newOffset = Vector2.Distance(SelectedBrush.StartPoint,
                                                         localStopPos) /
                                        Vector2.Distance(
                                            SelectedBrush.StartPoint,
                                            SelectedBrush.EndPoint);

                        Move(_selection.ToArray(), newOffset - stop.Offset,
                             ModifyGradientCommand.GradientOperation.ChangeOffset);
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
            if (SelectedBrush == null)
                return;

            var transform = SelectedLayer.AbsoluteTransform;
            var t = new Func<Vector2, Vector2>(v => Vector2.Transform(v, transform));
            var zoom = view.Zoom;

            var p = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutline)));
            var p2 = target.CreatePen(1, cacheManager.GetBrush(nameof(EditorColors.NodeOutlineAlt)));

            Vector2 start = t(SelectedBrush.StartPoint),
                    end = t(SelectedBrush.EndPoint);

            target.DrawLine(start, end, p);

            target.FillCircle(start, 3, cacheManager.GetBrush(nameof(EditorColors.Node)));
            target.DrawCircle(start, 3, p);

            target.FillCircle(end, 3, cacheManager.GetBrush(nameof(EditorColors.Node)));
            target.DrawCircle(end, 3, p);

            foreach (var stop in SelectedBrush.Stops)
            {
                var pos = Vector2.Lerp(start, end, stop.Offset);

                target.FillCircle(pos, 7, cacheManager.GetBrush(nameof(EditorColors.Node)));
                target.DrawCircle(pos, 7, p);

                using (var brush = target.CreateBrush(stop.Color))
                    target.FillCircle(pos, 4, brush);
            }

            // do not dispose the brushes! they are being used by the cache manager
            // and do not automatically regenerated b/c they are resource brushes
            p?.Dispose();
            p2?.Dispose();
        }

        public bool TextInput(string text) { return false; }

        public string CursorImage => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status =>
            "<b>Click</b> to select, " +
            "<b>Alt Click</b> to delete, " +
            "<b>Shift Click</b> to multi-select.";

        public ToolType Type => ToolType.Gradient;

        #endregion
    }
}