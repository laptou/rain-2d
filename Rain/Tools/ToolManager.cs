using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;

namespace Rain.Tools
{
    public class ToolManager : Core.Model.Model, IToolManager
    {
        public ToolManager(IArtContext artView) { Context = artView; }

        public void SetTool(ToolType type)
        {
            lock (this)
            {
                Tool?.Detach(Context);

                switch (type)
                {
                    case ToolType.Select:
                        Tool = new SelectionTool(this);

                        break;
                    case ToolType.Node:
                        Tool = new NodeTool(this);

                        break;
                    case ToolType.Pencil:
                        Tool = new PencilTool(this);

                        break;
                    case ToolType.Text:
                        Tool = new TextTool(this);

                        break;
                    case ToolType.Gradient:
                        Tool = new GradientTool(this);

                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            Tool.Attach(Context);


            RaisePropertyChanged(nameof(Type));
            RaiseFillUpdate();
            RaiseStrokeUpdate();
            Context.InvalidateRender();
        }

        #region IToolManager Members

        public event EventHandler<IBrushInfo> FillUpdated;
        public event EventHandler<IPenInfo> StrokeUpdated;

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            // ToolManager doesn't subscribe to events from any other managers.
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            // ToolManager doesn't subscribe to events from any other managers.
        }

        public void RaiseFillUpdate() { FillUpdated?.Invoke(this, Tool?.ProvideFill()); }

        public void RaiseStatus(Status status) { Context.Status = status; }

        public void RaiseStrokeUpdate() { StrokeUpdated?.Invoke(this, Tool?.ProvideStroke()); }

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