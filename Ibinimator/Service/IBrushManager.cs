using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IBrushManager : IArtViewManager
    {
        BrushInfo Fill { get; set; }
        BrushInfo Stroke { get; set; }
        ObservableList<float> StrokeDashes { get; }
        StrokeStyleProperties1 StrokeStyle { get; set; }
        float StrokeWidth { get; set; }
    }
}