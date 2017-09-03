using System;
using System.Collections.Generic;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
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

        #region IToolManager Members

        public ArtView ArtView { get; }

        public void KeyDown(KeyEventArgs keyEventArgs)
        {
            Tool?.KeyDown(keyEventArgs.Key == Key.System ? keyEventArgs.SystemKey : keyEventArgs.Key);
        }

        public void KeyUp(KeyEventArgs keyEventArgs)
        {
            Tool?.KeyUp(keyEventArgs.Key == Key.System ? keyEventArgs.SystemKey : keyEventArgs.Key);
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

        #endregion

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

        private IEnumerable<Layer> Selection => Manager.ArtView.SelectionManager.Selection;

        #region ITool Members

        public Bitmap Cursor => Manager.ArtView.SelectionManager.Cursor;

        public float CursorRotate => Manager.ArtView.SelectionManager.SelectionRotation -
                                     Manager.ArtView.SelectionManager.SelectionShear;

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

        public IToolManager Manager { get; }

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

        public string Status => "";

        public ToolType Type => ToolType.Select;

        #endregion
    }

    public sealed class PencilTool : Model.Model, ITool
    {
        private readonly List<PathNode> _selectedNodes = new List<PathNode>();
        private bool _alt;
        private Vector2 _lastPos;
        private bool _shift;

        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public Path CurrentPath => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Path;

        private ArtView ArtView => Manager.ArtView;

        private Layer Root => ArtView.ViewManager.Root;

        #region ITool Members

        public Bitmap Cursor => null;

        public float CursorRotate => 0;

        public void KeyDown(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
                _shift = true;

            if (key == Key.LeftAlt || key == Key.RightAlt)
                _alt = true;

            if (key == Key.Escape)
            {
                Manager.ArtView.SelectionManager.ClearSelection();
                _selectedNodes.Clear();
            }

            if (key == Key.Delete)
            {
                foreach (var node in _selectedNodes)
                    CurrentPath?.Nodes.Remove(node);
                _selectedNodes.Clear();

                Manager.ArtView.InvalidateSurface();
            }
        }

        public void KeyUp(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
                _shift = false;
            if (key == Key.LeftAlt || key == Key.RightAlt)
                _alt = false;
        }

        public IToolManager Manager { get; }

        public void MouseDown(Vector2 pos)
        {
            if (CurrentPath == null)
            {
                var hit = Root.Hit<Path>(ArtView.RenderTarget.Factory, pos, Matrix3x2.Identity, true);

                if (hit != null)
                {
                    hit.Selected = true;
                    return;
                }

                Manager.ArtView.SelectionManager.ClearSelection();

                var path = new Path
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

            var tpos =
                Matrix3x2.TransformPoint(
                    Matrix3x2.Invert(CurrentPath.AbsoluteTransform), pos);

            var node = CurrentPath.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 5);

            if (node != null)
            {
                if (_shift)
                {
                    _selectedNodes.Add(node);
                }
                else
                {
                    var figures = CurrentPath.Nodes.Split(n => n is CloseNode);
                    var index = 0;

                    foreach (var figure in figures.Select(Enumerable.ToArray))
                    {
                        var start = figure.FirstOrDefault();

                        if (start == null) continue;

                        index += figure.Length;

                        if (start == node)
                            if (CurrentPath.Nodes.ElementAtOrDefault(index) is CloseNode close)
                                close.Open = !close.Open;
                            else
                                CurrentPath.Nodes.Insert(index, new CloseNode());
                    }

                    _selectedNodes.Clear();
                    _selectedNodes.Add(node);
                }
            }
            else
            {
                PathNode newNode;

                if (_shift)
                {
                    var cpos =
                        Matrix3x2.TransformPoint(
                            Matrix3x2.Invert(CurrentPath.AbsoluteTransform),
                            Constrain(pos));

                    newNode = new PathNode {X = cpos.X, Y = cpos.Y};
                }
                else
                {
                    newNode = new PathNode {X = tpos.X, Y = tpos.Y};
                }

                if (_selectedNodes.Count >= 2 && _alt)
                {
                    var last = _selectedNodes[_selectedNodes.Count - 1];
                    var second = _selectedNodes[_selectedNodes.Count - 2];

                    var lastIndex = CurrentPath.Nodes.IndexOf(last);
                    var secondIndex = CurrentPath.Nodes.IndexOf(second);

                    if (Math.Abs(lastIndex - secondIndex) == 1)
                    {
                        CurrentPath.Nodes.Insert(lastIndex - (lastIndex - secondIndex) + 1, newNode);
                        _selectedNodes.Insert(_selectedNodes.Count - 1, newNode);
                    }
                }
                else
                {
                    CurrentPath.Nodes.Add(newNode);
                    _selectedNodes.Clear();
                }
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

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            if (CurrentPath == null) return;

            var props = new StrokeStyleProperties1 {TransformType = StrokeTransformType.Fixed};
            using (var stroke = new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), props))
            {
                var transform = CurrentPath.AbsoluteTransform;

                using (var geom = cacheManager.GetGeometry(CurrentPath))
                {
                    target.Transform *= transform;
                    target.DrawGeometry(geom, cacheManager.GetBrush("A2"), 1, stroke);
                    target.Transform *= Matrix3x2.Invert(transform);
                }

                var figures = CurrentPath.Nodes.Split(n => n is CloseNode);

                foreach (var figure in figures)
                {
                    var nodes = figure.ToArray();

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];

                        var pos = Matrix3x2.TransformPoint(transform, node.Position);
                        var dist = (pos - _lastPos).LengthSquared();

                        if (_selectedNodes.Contains(node) ||
                            _selectedNodes.Contains(nodes.ElementAtOrDefault(MathUtil.Wrap(i - 1, 0, nodes.Length))) ||
                            _selectedNodes.Contains(nodes.ElementAtOrDefault(MathUtil.Wrap(i + 1, 0, nodes.Length))))
                            if (node is CubicPathNode cn)
                            {
                                target.DrawEllipse(new Ellipse
                                {
                                    Point = Matrix3x2.TransformPoint(transform, cn.Control1),
                                    RadiusX = 3,
                                    RadiusY = 3
                                }, cacheManager.GetBrush("A2"), 1, stroke);

                                target.DrawEllipse(new Ellipse
                                {
                                    Point = Matrix3x2.TransformPoint(transform, cn.Control2),
                                    RadiusX = 3,
                                    RadiusY = 3
                                }, cacheManager.GetBrush("A2"), 1, stroke);
                            }
                            else if (node is QuadraticPathNode qn)
                            {
                                target.DrawEllipse(new Ellipse
                                {
                                    Point = Matrix3x2.TransformPoint(transform, qn.Control),
                                    RadiusX = 3,
                                    RadiusY = 3
                                }, cacheManager.GetBrush("A2"), 1, stroke);
                            }

                        var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8, 8);

                        if (_selectedNodes.Contains(node))
                            target.FillRectangle(rect,
                                rect.Contains(_lastPos) ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A3"));
                        else
                            target.FillRectangle(rect,
                                rect.Contains(_lastPos) ? cacheManager.GetBrush("L3") : cacheManager.GetBrush("L1"));

                        target.DrawRectangle(rect,
                            i == 0 ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A2"),
                            1, stroke);
                    }
                }
            }
        }

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion

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