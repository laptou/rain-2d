using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class SelectTool : Core.Model.Model, ITool
    {
        private bool _down;

        private readonly Dictionary<string, string> _statuses = new Dictionary<string, string>
        {
            {
                "default",
                "<b>Alt Click</b> to select-behind. " +
                "<b>Shift Click</b> to multi-select."
            },
            {
                "selection",
                "<b>{0}</b> layer(s) selected " +
                "[{1}]"
            },
            {
                "transform",
                "<b>Shift-Drag</b> for constrained movement. " +
                "<b>Alt-Shift-Drag</b> for locally constrained movement."
            }
        };

        public SelectTool(IToolManager toolManager, ISelectionManager selectionManager)
        {
            Manager = toolManager;

            Status = _statuses["default"];

            selectionManager.Updated += (sender, args) => { UpdateStatus(); };
        }

        private void UpdateStatus()
        {
            if (Selection.Count > 0)
            {
                if (!_down)
                {
                    var names = Selection.Select(l => l.Name ?? l.DefaultName)
                                         .ToArray();

                    Status = string.Format(
                        _statuses["selection"],
                        names.Length,
                        string.Join(", ", names));
                }
                else Status = _statuses["transform"];

                return;
            }

            Status = _statuses["default"];  
        }

        public IToolOption[] Options => new IToolOption[0];

        private IList<ILayer> Selection => Manager.Context.SelectionManager.Selection;

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            if (Selection.Count == 0)
                return;

            var targets =
                Selection.SelectMany(l => (l as Layer).Flatten())
                         .OfType<IFilledLayer>()
                         .ToArray();

            var command = new ApplyFillCommand(
                Manager.Context.HistoryManager.Position + 1,
                targets,
                brush,
                targets.Select(t => t.Fill).ToArray());

            var old = Manager.Context.HistoryManager.Current;

            if (old is ApplyFillCommand oldFillCommand && command.Time - old.Time <= 500)
            {
                Manager.Context.HistoryManager.Pop();

                command = new ApplyFillCommand(
                    command.Id,
                    command.Targets,
                    command.NewFill,
                    oldFillCommand.OldFills);
            }

            Manager.Context.HistoryManager.Do(command);
        }

        public void ApplyStroke(PenInfo pen)
        {
            if (Selection.Count == 0)
                return;

            var targets =
                Selection.SelectMany(l => (l as Layer).Flatten())
                         .OfType<IStrokedLayer>()
                         .ToArray();

            var command = new ApplyStrokeCommand(
                Manager.Context.HistoryManager.Position + 1,
                targets,
                pen,
                targets.Select(t => t.Stroke).ToArray());

            var old = Manager.Context.HistoryManager.Current;

            if (old is ApplyStrokeCommand oldStrokeCommand &&
                command.Time - old.Time <= 500)
            {
                Manager.Context.HistoryManager.Pop();

                command = new ApplyStrokeCommand(
                    command.Id,
                    command.Targets,
                    command.NewStroke,
                    oldStrokeCommand.OldStrokes);
            }

            Manager.Context.HistoryManager.Do(command);
        }

        public void Dispose() { }

        public bool KeyDown(Key key, ModifierKeys modifier)
        {
            if (key == Key.Delete)
            {
                var delete = Selection.ToArray();
                Manager.Context.SelectionManager.ClearSelection();

                foreach (var layer in delete)
                    Manager.Context.HistoryManager.Do(
                        new RemoveLayerCommand(
                            Manager.Context.HistoryManager.Position + 1,
                            layer.Parent,
                            layer));

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key, ModifierKeys modifier) { return false; }

        public bool MouseDown(Vector2 pos)
        {
            _down = true;
            UpdateStatus();
            return false;
        }

        public bool MouseMove(Vector2 pos) { return false; }

        public bool MouseUp(Vector2 pos)
        {
            _down = false;
            UpdateStatus();
            return false;
        }

        public void Render(RenderContext target, ICacheManager cache)
        {
            var rect = Manager.Context.SelectionManager.SelectionBounds;

            if (rect.IsEmpty) return;

            // draw handles
            var handles = new List<Vector2>();

            float x1 = rect.Left,
                  y1 = rect.Top,
                  x2 = rect.Right,
                  y2 = rect.Bottom;

            Vector2 Transform(float x, float y) =>
                Manager.Context.SelectionManager.FromSelectionSpace(new Vector2(x, y));

            handles.Add(Transform(x1, y1));
            handles.Add(Transform(x2, y1));
            handles.Add(Transform(x2, y2));
            handles.Add(Transform(x1, y2));

            var top = Transform((x1 + x2) / 2, y1);
            handles.Add(top);

            handles.Add(Transform(x1, (y1 + y2) / 2));
            handles.Add(Transform(x2, (y1 + y2) / 2));

            var bottom = Transform((x1 + x2) / 2, y2);
            handles.Add(bottom);
            handles.Add(top - Vector2.Normalize(bottom - top) * 15);

            var zoom = Vector2.One; // MathUtils.GetScale(target.Transform);

            using (var pen = target.CreatePen(2, cache.GetBrush("L1")))
            {
                foreach (var v in handles)
                {
                    target.FillEllipse(v, 5f / zoom.Y, 5f / zoom.X, cache.GetBrush("A1"));
                    target.DrawEllipse(v, 5f / zoom.Y, 5f / zoom.X, pen);
                }
            }
        }

        public bool TextInput(string text) { return false; }

        public string Cursor => Manager.Context.SelectionManager.Cursor;

        public float CursorRotate =>
            Manager.Context.SelectionManager.SelectionTransform.GetRotation();

        public IToolManager Manager { get; }

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public ToolType Type => ToolType.Select;

        #endregion
    }
}