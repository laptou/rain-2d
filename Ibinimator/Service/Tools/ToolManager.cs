using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Tools
{
    public class ToolManager : Core.Model.Model, IToolManager
    {
        public ToolManager(IArtContext artView, ISelectionManager selectionManager)
        {
            Context = artView;
        }

        public void RaiseStrokeUpdate() { throw new NotImplementedException(); }

        public void SetTool(ToolType type)
        {
            lock (this)
            {
                Tool?.Dispose();
                Tool = null;

                switch (type)
                {
                    case ToolType.Select:
                        Tool = new SelectTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Node:
                        Tool = new NodeTool(this);
                        break;
                    case ToolType.Pencil:
                        Tool = new PencilTool(this);
                        break;
                    case ToolType.Pen:
                        break;
                    case ToolType.Eyedropper:
                        break;
                    case ToolType.Flood:
                        break;
                    case ToolType.Keyframe:
                        break;
                    case ToolType.Text:
                        Tool = new TextTool(this);
                        break;
                    case ToolType.Mask:
                        break;
                    case ToolType.Zoom:
                        break;
                    case ToolType.Gradient:
                        Tool = new GradientTool(this);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            RaisePropertyChanged(nameof(Type));
            Context.InvalidateSurface();
        }

        public event EventHandler<BrushInfo> FillUpdated;
        public event EventHandler<BrushInfo> StrokeUpdated;

        #region IToolManager Members

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            lock (this)
            {
                return Tool?.KeyDown(key, modifiers) == true;
            }
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            lock (this)
            {
                return Tool?.KeyUp(key, modifiers) == true;
            }
        }

        public bool MouseDown(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseDown(pos) == true;
            }
        }

        public bool MouseMove(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseMove(pos) == true;
            }
        }

        public bool MouseUp(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseUp(pos) == true;
            }
        }

        public bool TextInput(string text)
        {
            lock (this)
            {
                return Tool?.TextInput(text) == true;
            }
        }

        public void RaiseStatus(Status status)
        {
            Context.Status = status;
        }

        public void RaiseFillUpdate() { FillUpdated?.Invoke(this, Tool.ProvideFill()); }

        public IArtContext Context { get; }

        public ITool Tool
        {
            get => Get<ITool>();
            private set
            {
                RaisePropertyChanged(nameof(Type));
                Set(value);
            }
        }

        public ToolType Type
        {
            get => Tool?.Type ?? ToolType.Select;
            set => SetTool(value);
        }

        #endregion
    }

    
}