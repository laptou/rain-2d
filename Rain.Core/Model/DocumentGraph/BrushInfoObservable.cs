using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.DocumentGraph
{
    public static class BrushInfoObservable
    {
        public static IObservable<Color> CreateColorObservable(this ISolidColorBrushInfo brush)
        {
            return brush.CreateObservable(b => b.Color);
        }

        public static IObservable<Vector2> CreateEndPointObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(b => b.EndPoint);
        }

        public static IObservable<GradientBrushType> CreateGradientTypeObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(b => b.Type);
        }

        public static IObservable<float> CreateOpacityObservable(this IBrushInfo brush)
        {
            return brush.CreateObservable(b => b.Opacity);
        }

        public static IObservable<Vector2> CreateStartPointObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(b => b.StartPoint);
        }

        public static IObservable<IEnumerable<GradientStop>> CreateStopsObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(b => b.Stops);
        }

        public static IObservable<Matrix3x2> CreateTransformObservable(this IBrushInfo brush)
        {
            return brush.CreateObservable(b => b.Transform);
        }
    }
}