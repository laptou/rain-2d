using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.View.Control;
using SharpDX;

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

        public ITool Tool { get; set; }

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
    }
}
