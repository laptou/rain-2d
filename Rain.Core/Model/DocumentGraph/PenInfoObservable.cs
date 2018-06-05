using System;
using System.Collections.Generic;
using System.Linq;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.DocumentGraph
{
    public static class PenInfoObservable
    {
        public static IObservable<IBrushInfo> CreateBrushObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.Brush), p => p.Brush);
        }

        public static IObservable<IEnumerable<float>> CreateDashesObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.Dashes), p => p.Dashes, SequenceEqualEqualityComparer<float>.Instance);
        }

        public static IObservable<float> CreateDashOffsetObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.DashOffset), p => p.DashOffset, EpsilonComparer.Instance);
        }

        public static IObservable<LineCap> CreateLineCapObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.LineCap), p => p.LineCap);
        }

        public static IObservable<LineJoin> CreateLineJoinObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.LineJoin), p => p.LineJoin);
        }

        public static IObservable<float> CreateMiterLimitObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.MiterLimit), p => p.MiterLimit, EpsilonComparer.Instance);
        }

        public static IObservable<float> CreateWidthObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(nameof(IPenInfo.Width), p => p.Width, EpsilonComparer.Instance);
        }
    }
}