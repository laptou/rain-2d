using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IToolManager : IArtContextManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }

        event EventHandler<IBrushInfo> FillUpdated;
        event EventHandler<IPenInfo> StrokeUpdated;

        void RaiseFillUpdate();
        void RaiseStatus(Status status);
        void RaiseStrokeUpdate();
    }
}