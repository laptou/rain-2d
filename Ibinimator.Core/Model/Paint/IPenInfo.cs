using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Utility;

namespace Ibinimator.Core.Model.Paint
{
    public interface IPenInfo : IModel
    {
        IBrushInfo Brush { get; set; }
        ObservableList<float> Dashes { get; set; }
        float DashOffset { get; set; }
        bool HasDashes { get; set; }
        LineCap LineCap { get; set; }
        LineJoin LineJoin { get; set; }
        float MiterLimit { get; set; }
        float Width { get; set; }

        IPen CreatePen(RenderContext renderCtx);
    }
}