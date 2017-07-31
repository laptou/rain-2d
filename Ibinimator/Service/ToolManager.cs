﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

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
        }
    }

    public class SelectTool : Model.Model, ITool
    {
        public SelectTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public IToolManager Manager { get; }
        public ToolType Type => ToolType.Select;
        public string Status => "";

        public void MouseDown(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.OnMouseDown(pos);
        }

        public void MouseMove(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.OnMouseMove(pos);
        }

        public void MouseUp(Vector2 pos)
        {
            Manager.ArtView.SelectionManager.OnMouseUp(pos);
        }

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            // rendering is handled by SelectionManager
        }
    }

    public class PencilTool : Model.Model, ITool
    {
        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public IToolManager Manager { get; }
        public ToolType Type => ToolType.Select;
        public string Status => "";

        public Path CurrentPath => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Path;

        public void MouseDown(Vector2 pos)
        {
            if (CurrentPath == null)
            {
                Manager.ArtView.SelectionManager.ClearSelection();

                Path path = new Path();
                path.FillBrush = Manager.ArtView.BrushManager.Fill;
                path.StrokeBrush = Manager.ArtView.BrushManager.Stroke;
                path.StrokeWidth = Manager.ArtView.BrushManager.StrokeWidth;
                path.StrokeStyle = Manager.ArtView.BrushManager.StrokeStyle;

                Manager.ArtView.ViewManager.Root.Add(path);

                path.Selected = true;
            }
            else
            {
                CurrentPath.Nodes.Add(new PathNode { X = pos.X, Y = pos.Y });
            }
        }

        public void MouseMove(Vector2 pos)
        {
        }

        public void MouseUp(Vector2 pos)
        {
        }

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            if (CurrentPath == null) return;

            var props = new StrokeStyleProperties1 { TransformType = StrokeTransformType.Hairline };
            using (StrokeStyle1 stroke = 
                new StrokeStyle1(target.Factory.QueryInterface<Factory1>(), props))
            {
                var transform = CurrentPath.AbsoluteTransform;

                using (Geometry geom = CurrentPath.GetGeometry(target.Factory))
                {
                    target.Transform *= transform;
                    target.DrawGeometry(geom, cacheManager.GetBrush("A2"), 1, stroke);
                    target.Transform *= Matrix3x2.Invert(transform);
                }

                foreach (var node in 
                    CurrentPath.Nodes.Select(n => 
                        Matrix3x2.TransformPoint(transform, n.Position)))
                {
                    RawRectangleF rect = new RawRectangleF(
                        node.X - 2.5f, node.Y - 2.5f, node.X + 2.5f, node.Y + 2.5f);
                    target.DrawRectangle(rect, cacheManager.GetBrush("A2"), 1, stroke);
                }
            }
        }
    }
}
