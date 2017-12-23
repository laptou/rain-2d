using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;

namespace Ibinimator.Service.Tools
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
                        Tool = new SelectionTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Node:
                        Tool = new NodeTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Pencil:
                        Tool = new PencilTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Text:
                        Tool = new TextTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Gradient:
                        Tool = new GradientTool(this, Context.SelectionManager);
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
        public event EventHandler<IPenInfo>   StrokeUpdated;

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
    }
}