using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.Utility;
using SharpDX;
using SharpDX.Direct2D1;
using Ellipse = SharpDX.Direct2D1.Ellipse;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service.Tools
{
    public sealed class SelectTool : Model.Model, ITool
    {
        public SelectTool(IToolManager toolManager, ISelectionManager selectionManager)
        {
            Manager = toolManager;
            Status = "lel";

            selectionManager.Updated += (sender, args) =>
            {
                var names = selectionManager.Selection.Select(l => l.Name ?? l.DefaultName)
                                                      .ToArray();

                Status = $"{selectionManager.Selection.Count} layer(s) selected " +
                         $"[{string.Join(", ", names.Take(6))}{(names.Length > 6 ? "..." : "")}]";
            };
        }

        private IEnumerable<Layer> Selection => Manager.ArtView.SelectionManager.Selection;

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            var targets =
                Selection.SelectMany(l => l.Flatten())
                    .OfType<IFilledLayer>()
                    .ToArray();

            var command = new ApplyFillCommand(
                Manager.ArtView.HistoryManager.Position + 1,
                targets, brush,
                targets.Select(t => t.FillBrush).ToArray());

            Manager.ArtView.HistoryManager.Do(command);
        }

        public void ApplyStroke(BrushInfo brush, StrokeInfo stroke)
        {
            var targets =
                Selection.SelectMany(l => l.Flatten())
                    .OfType<IStrokedLayer>()
                    .ToArray();

            var command = new ApplyStrokeCommand(
                Manager.ArtView.HistoryManager.Position + 1,
                targets,
                brush, targets.Select(t => t.StrokeBrush).ToArray(),
                stroke, targets.Select(t => t.StrokeInfo).ToArray());

            Manager.ArtView.HistoryManager.Do(command);
        }

        public void Dispose()
        {
        }

        public bool KeyDown(Key key)
        {
            if (key == Key.Delete)
            {
                var delete = Selection.ToArray();
                Manager.ArtView.SelectionManager.ClearSelection();

                foreach (var layer in delete)
                    Manager.ArtView.HistoryManager.Do(
                        new RemoveLayerCommand(Manager.ArtView.HistoryManager.Position + 1,
                            layer.Parent,
                            layer));

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key)
        {
            return false;
        }

        public bool MouseDown(Vector2 pos)
        {
            return false;
        }

        public bool MouseMove(Vector2 pos)
        {
            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            return false;
        }

        public void Render(RenderTarget target, ICacheManager cache)
        {
            var rect = Manager.ArtView.SelectionManager.SelectionBounds;

            if (rect.IsEmpty) return;

            // draw handles
            var handles = new List<Vector2>();

            float x1 = rect.Left,
                y1 = rect.Top,
                x2 = rect.Right,
                y2 = rect.Bottom;

            handles.Add(new Vector2(x1, y1));
            handles.Add(new Vector2(x2, y1));
            handles.Add(new Vector2(x2, y2));
            handles.Add(new Vector2(x1, y2));
            handles.Add(new Vector2((x1 + x2) / 2, y1));
            handles.Add(new Vector2(x1, (y1 + y2) / 2));
            handles.Add(new Vector2(x2, (y1 + y2) / 2));
            handles.Add(new Vector2((x1 + x2) / 2, y2));
            handles.Add(new Vector2((x1 + x2) / 2, y1 - 10));

            var zoom = MathUtils.GetScale(target.Transform);

            using (var stroke =
                new StrokeStyle1(
                    target.Factory.QueryInterface<Factory1>(),
                    new StrokeStyleProperties1
                    {
                        TransformType = StrokeTransformType.Fixed
                    }))
            {
                foreach (var v in handles.Select(Manager.ArtView.SelectionManager.ToSelectionSpace))
                {
                    var e = new Ellipse(v, 5f / zoom.Y, 5f / zoom.X);
                    target.FillEllipse(e, cache.GetBrush("A1"));
                    target.DrawEllipse(e, cache.GetBrush("L1"), 2, stroke);
                }
            }
        }

        public Bitmap Cursor => Manager.ArtView.SelectionManager.Cursor;

        public float CursorRotate => Manager.ArtView.SelectionManager.SelectionRotation -
                                     Manager.ArtView.SelectionManager.SelectionShear;

        public IToolManager Manager { get; }

        public ToolOption[] Options => new ToolOption[0];

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public ToolType Type => ToolType.Select;

        #endregion
    }
}