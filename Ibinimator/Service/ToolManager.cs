using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Shared;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using Ellipse = SharpDX.Direct2D1.Ellipse;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public class ToolManager : Model.Model, IToolManager
    {
        public ToolManager(ArtView artView)
        {
            ArtView = artView;
            SetTool(ToolType.Select);
        }

        public ArtView ArtView { get; }

        public ITool Tool
        {
            get => Get<ITool>();
            set => Set(value);
        }

        public ToolType Type
        {
            get => Tool.Type;
            set => SetTool(value);
        }

        public void MouseDown(Vector2 pos)
        {
            Tool?.MouseDown(pos);
        }

        public void MouseMove(Vector2 pos)
        {
            Tool?.MouseMove(pos);
        }

        public void MouseUp(Vector2 pos)
        {
            Tool?.MouseUp(pos);
        }

        public void KeyDown(KeyEventArgs keyEventArgs)
        {
            Tool?.KeyDown(keyEventArgs.Key == Key.System ? keyEventArgs.SystemKey : keyEventArgs.Key);
        }

        public void KeyUp(KeyEventArgs keyEventArgs)
        {
            Tool?.KeyUp(keyEventArgs.Key == Key.System ? keyEventArgs.SystemKey : keyEventArgs.Key);
        }

        public void SetTool(ToolType type)
        {
            switch (type)
            {
                case ToolType.Select:
                    Tool = new SelectTool(this);
                    break;
                case ToolType.Path:
                    break;
                case ToolType.Pencil:
                    Tool = new PencilTool(this);
                    break;
                case ToolType.Pen:
                    break;
                case ToolType.Eyedropper:
                    break;
                case ToolType.Bucket:
                    break;
                case ToolType.Timeline:
                    break;
                case ToolType.Text:
                    break;
                case ToolType.Mask:
                    break;
                case ToolType.Zoom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            RaisePropertyChanged(nameof(Type));
            ArtView.InvalidateSurface();
        }
    }

    public sealed class SelectTool : Model.Model, ITool
    {
        public SelectTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public IToolManager Manager { get; }

        public ToolType Type => ToolType.Select;

        public string Status => "";

        public Bitmap Cursor => Manager.ArtView.SelectionManager.Cursor;

        public float CursorRotate => Manager.ArtView.SelectionManager.SelectionRotation -
                                     Manager.ArtView.SelectionManager.SelectionShear;

        private IEnumerable<Layer> Selection => Manager.ArtView.SelectionManager.Selection;

        public void MouseDown(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.MouseDown(pos);
        }

        public void MouseMove(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.MouseMove(pos);
        }

        public void MouseUp(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.MouseUp(pos);
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

        public void KeyDown(Key key)
        {
            if (key == Key.Delete)
            {
                var delete = Selection.ToArray();
                Manager.ArtView.SelectionManager.ClearSelection();

                Manager.ArtView.Dispatcher.Invoke(() =>
                {
                    foreach (var layer in delete)
                        layer.Parent.Remove(layer);
                });

            }
        }

        public void KeyUp(Key key)
        {
            
        }
    }

    public sealed class PencilTool : Model.Model, ITool
    {
        private Vector2 _lastPos;
        private bool _shift;
        private bool _alt;

        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public IToolManager Manager { get; }

        private ArtView ArtView => Manager.ArtView;

        private Layer Root => ArtView.ViewManager.Root;

        public ToolType Type => ToolType.Pencil;

        public string Status => "";

        public Bitmap Cursor => null;

        public float CursorRotate => 0;

        public Path CurrentPath => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Path;

        public void MouseDown(Vector2 pos)
        {
            if (CurrentPath == null)
            {
                var hit = Root.Hit<Path>(ArtView.RenderTarget.Factory, pos, Matrix3x2.Identity);

                if (hit != null)
                {
                    hit.Selected = true;
                    return;
                }

                Manager.ArtView.SelectionManager.ClearSelection();

                Path path = new Path
                {
                    FillBrush = Manager.ArtView.BrushManager.Fill,
                    StrokeBrush = Manager.ArtView.BrushManager.Stroke,
                    StrokeWidth = Manager.ArtView.BrushManager.StrokeWidth,
                    StrokeStyle = Manager.ArtView.BrushManager.StrokeStyle
                };

                Manager.ArtView.Dispatcher.Invoke(() =>
                    Manager.ArtView.ViewManager.Root.Add(path));

                path.Selected = true;
            }

            Debug.Assert(CurrentPath != null, "CurrentPath != null");

            var tpos =
                Matrix3x2.TransformPoint(
                    Matrix3x2.Invert(CurrentPath.AbsoluteTransform), pos);

            if (_alt)
            {
                var node = CurrentPath.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 14.12f);

                if (node != null)
                    CurrentPath.Nodes.Remove(node);
            }
            else if (_shift && CurrentPath.Nodes.Count > 0)
            {
                var cpos = 
                    Matrix3x2.TransformPoint(
                        Matrix3x2.Invert(CurrentPath.AbsoluteTransform),
                        Constrain(pos));

                CurrentPath.Nodes.Add(new PathNode { X = cpos.X, Y = cpos.Y });
            }
            else
            {
                var first = CurrentPath.Nodes.FirstOrDefault();

                if (first != null && (first.Position - tpos).Length() < 14.12f)
                    CurrentPath.Closed = !CurrentPath.Closed;
                else
                    CurrentPath.Nodes.Add(new PathNode { X = tpos.X, Y = tpos.Y });
            }
            

            Manager.ArtView.SelectionManager.Update(true);
        }

        public void MouseMove(Vector2 pos)
        {
            _lastPos = pos;

            Manager.ArtView.InvalidateSurface();
        }

        public void MouseUp(Vector2 pos)
        {
        }

        public void KeyDown(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
                _shift = true;
            if (key == Key.LeftAlt || key == Key.RightAlt)
                _alt = true;
            if(key == Key.Escape)
                Manager.ArtView.SelectionManager.ClearSelection();
        }

        public void KeyUp(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
                _shift = false;
            if (key == Key.LeftAlt || key == Key.RightAlt)
                _alt = false;
        }

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            if (CurrentPath == null) return;

            var props = new StrokeStyleProperties1 { TransformType = StrokeTransformType.Fixed };
            using (var stroke = new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), props))
            {
                var transform = CurrentPath.AbsoluteTransform;

                using (var geom = cacheManager.GetGeometry(CurrentPath))
                {
                    target.Transform *= transform;
                    target.DrawGeometry(geom, cacheManager.GetBrush("A2"), 1, stroke);
                    target.Transform *= Matrix3x2.Invert(transform);
                }

                if (CurrentPath.Nodes.Count > 0)
                {
                    var lpos = 
                        Matrix3x2.TransformPoint(
                            CurrentPath.AbsoluteTransform,
                            CurrentPath.Nodes.Last().Position);
                    
                    var mpos = _shift ? Constrain(_lastPos) : _lastPos;

                    target.DrawLine(lpos, mpos, cacheManager.GetBrush("A2"), 1, stroke);

                    if (CurrentPath.Closed)
                    {
                        var fpos =
                            Matrix3x2.TransformPoint(
                                CurrentPath.AbsoluteTransform,
                                CurrentPath.Nodes.First().Position);

                        target.DrawLine(mpos, fpos, cacheManager.GetBrush("A2"), 1, stroke);
                    }
                }

                foreach (var node in 
                    CurrentPath.Nodes.Select(n => 
                        Matrix3x2.TransformPoint(transform, n.Position)))
                {
                    var rect = new RectangleF(node.X - 5f, node.Y - 5f, 10, 10);

                    target.FillRectangle(rect,
                        rect.Contains(_lastPos) ?
                            cacheManager.GetBrush("A4") :
                            cacheManager.GetBrush("L1"));
                    target.DrawRectangle(rect, cacheManager.GetBrush("A2"), 1, stroke);
                }
            }
        }

        private Vector2 Constrain(Vector2 pos)
        {
            var lastNode = CurrentPath.Nodes.Last();
            var lpos = Matrix3x2.TransformPoint(CurrentPath.AbsoluteTransform, lastNode.Position);

            var delta = pos - lpos;

            if (Math.Abs(delta.Y / delta.X) > MathUtils.Sqrt3)
                delta = new Vector2(0, delta.Y);
            else if (Math.Abs(delta.Y / delta.X) > MathUtils.InverseSqrt3)
                delta = MathUtils.Project(delta, new Vector2(1, Math.Sign(delta.Y / delta.X)));
            else
                delta = new Vector2(delta.X, 0);

            return lpos + delta;
        }
    }
}
