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
            return brush.CreateObservable(nameof(ISolidColorBrushInfo.Color), b => b.Color, EpsilonComparer.Instance);
        }

        public static IObservable<Vector2> CreateEndPointObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(nameof(IGradientBrushInfo.EndPoint),
                                          b => b.EndPoint,
                                          EpsilonComparer.Instance);
        }

        public static IObservable<GradientBrushType> CreateGradientTypeObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(nameof(IGradientBrushInfo.Type), b => b.Type);
        }

        public static IObservable<float> CreateOpacityObservable(this IBrushInfo brush)
        {
            return brush.CreateObservable(nameof(IBrushInfo.Opacity), b => b.Opacity, EpsilonComparer.Instance);
        }

        public static IObservable<Vector2> CreateStartPointObservable(this IGradientBrushInfo brush)
        {
            return brush.CreateObservable(nameof(IGradientBrushInfo.StartPoint),
                                          b => b.StartPoint,
                                          EpsilonComparer.Instance);
        }

        public static IObservable<IEnumerable<GradientStop>> CreateStopsObservable(this IGradientBrushInfo brush)
        {
            var item = new LambdaEqualityComparer<GradientStop>(
                (g1, g2) => EpsilonComparer.Instance.Equals(g1.Offset, g2.Offset) &&
                            EpsilonComparer.Instance.Equals(g1.Color, g2.Color));
            var sequence = new SequenceEqualEqualityComparer<GradientStop>(item);

            return brush.CreateObservable(nameof(IGradientBrushInfo.Stops), b => b.Stops, sequence);
        }

        public static IObservable<Matrix3x2> CreateTransformObservable(this IBrushInfo brush)
        {
            return brush.CreateObservable(nameof(IBrushInfo.Transform), b => b.Transform, EpsilonComparer.Instance);
        }
    }
}