using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    public interface IFilledLayer : ILayer
    {
        BrushInfo FillBrush { get; set; }
    }

    public interface IStrokedLayer : ILayer
    {
        BrushInfo StrokeBrush { get; set; }

        ObservableCollection<float> StrokeDashes { get; set; }

        StrokeStyleProperties1 StrokeStyle { get; set; }

        float StrokeWidth { get; set; }
    }

    public interface IGeometricLayer : IFilledLayer, IStrokedLayer
    {
        Geometry GetGeometry(Factory d2dFactory);
    }
}