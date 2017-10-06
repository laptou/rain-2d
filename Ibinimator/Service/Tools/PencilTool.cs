﻿using System;
using System.Collections.Generic;
using Ibinimator.Utility;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service.Tools
{
    public sealed class PencilTool : Model.Model, ITool
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

        public Path CurrentPath => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Path;

        private ArtView ArtView => Manager.ArtView;

        private IContainerLayer Root => ArtView.ViewManager.Root;

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

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            throw new NotImplementedException();
        }

        public void ApplyStroke(BrushInfo brush, StrokeInfo stroke)
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
                    ArtView.SelectionManager.ClearSelection();
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
                var hit = Root.Hit<Path>(ArtView.CacheManager, pos, true);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Manager.ArtView.SelectionManager.ClearSelection();

                var path = new Path
                {
                    FillBrush = Manager.ArtView.BrushManager.Fill,
                    StrokeBrush = Manager.ArtView.BrushManager.Stroke,
                    StrokeInfo = new StrokeInfo
                    {
                        Width = Manager.ArtView.BrushManager.StrokeWidth,
                        Style = Manager.ArtView.BrushManager.StrokeStyle,
                        Dashes = Manager.ArtView.BrushManager.StrokeDashes
                    }
                };

                Manager.ArtView.HistoryManager.Do(
                    new AddLayerCommand(Manager.ArtView.HistoryManager.Position + 1,
                        Root,
                        path));

                path.Selected = true;
            }

            _moved = false;
            _down = true;

            Manager.ArtView.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            _lastPos = pos;

            _moved = true;

            ArtView.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            if (!_moved)
            {
                var tpos =
                    Matrix3x2.TransformPoint(
                        Matrix3x2.Invert(CurrentPath.AbsoluteTransform), pos);

                var node = CurrentPath.Nodes.FirstOrDefault(n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    if (_alt)
                        ArtView.HistoryManager.Do(
                            new ModifyPathCommand(
                                ArtView.HistoryManager.Position + 1,
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
                            Matrix3x2.TransformPoint(
                                Matrix3x2.Invert(CurrentPath.AbsoluteTransform),
                                Constrain(pos));

                        newNode = new PathNode {X = cpos.X, Y = cpos.Y};
                    }
                    else
                    {
                        newNode = new PathNode {X = tpos.X, Y = tpos.Y};
                    }

                    Manager.ArtView.HistoryManager.Do(
                        new ModifyPathCommand(
                            Manager.ArtView.HistoryManager.Position + 1,
                            CurrentPath,
                            new[] {newNode},
                            CurrentPath.Nodes.Count,
                            ModifyPathCommand.NodeOperation.Add));
                }
            }

            ArtView.InvalidateSurface();

            _down = false;
            return true;
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
                    target.Transform = transform * target.Transform;
                    target.DrawGeometry(geom, cacheManager.GetBrush("A2"), 1, stroke);
                    target.Transform = Matrix3x2.Invert(transform) * target.Transform;
                }

                var figures = CurrentPath.Nodes.Split(n => n is CloseNode);

                foreach (var figure in figures)
                {
                    var nodes = figure.ToArray();

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var node = nodes[i];

                        var pos = Matrix3x2.TransformPoint(transform, node.Position);
                        var zoom = ArtView.ViewManager.Zoom;

                        var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8 / zoom, 8 / zoom);

                        if (_down)
                            target.FillRectangle(rect,
                                rect.Contains(_lastPos) ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("L1"));
                        else
                            target.FillRectangle(rect,
                                rect.Contains(_lastPos) ? cacheManager.GetBrush("A3") : cacheManager.GetBrush("L1"));

                        target.DrawRectangle(rect,
                            i == 0 ? cacheManager.GetBrush("A4") : cacheManager.GetBrush("A2"),
                            1, stroke);
                    }
                }
            }
        }

        public Bitmap Cursor => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public ToolOption[] Options => new ToolOption[0]; // TODO: add actual tool options

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion
    }
}