using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;

namespace Rain.Core
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