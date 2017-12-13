using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;

namespace Ibinimator.Service.Tools
{
    public class ToolManager : Model, IToolManager
    {
        public ToolManager(IArtContext artView) { Context = artView; }

        public void SetTool(ToolType type)
        {
            lock (this)
            {
                Tool?.Dispose();
                Tool = null;

                switch (type)
                {
                    case ToolType.Select:
                        Tool = new SelectionTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Node:
                        Tool = new NodeTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Pencil:
                        Tool = new PencilTool(this, Context.SelectionManager);
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
                        Tool = new TextTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Mask:
                        break;
                    case ToolType.Zoom:
                        break;
                    case ToolType.Gradient:
                        Tool = new GradientTool(this, Context.SelectionManager);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            RaisePropertyChanged(nameof(Type));
            Context.InvalidateSurface();
        }

        #region IToolManager Members

        public event EventHandler<IBrushInfo> FillUpdated;
        public event EventHandler<IPenInfo> StrokeUpdated;

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

        public void RaiseFillUpdate() { FillUpdated?.Invoke(this, Tool?.ProvideFill()); }

        public void RaiseStatus(Status status) { Context.Status = status; }

        public void RaiseStrokeUpdate() { StrokeUpdated?.Invoke(this, Tool?.ProvideStroke()); }

        public bool TextInput(string text)
        {
            lock (this)
            {
                return Tool?.TextInput(text) == true;
            }
        }

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