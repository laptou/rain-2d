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
            return pen.CreateObservable(p => p.Brush);
        }

        public static IObservable<IEnumerable<float>> CreateDashesObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.Dashes);
        }

        public static IObservable<float> CreateDashOffsetObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.DashOffset);
        }

        public static IObservable<LineCap> CreateLineCapObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.LineCap);
        }

        public static IObservable<LineJoin> CreateLineJoinObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.LineJoin);
        }

        public static IObservable<float> CreateMiterLimitObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.MiterLimit);
        }

        public static IObservable<float> CreateWidthObservable(this IPenInfo pen)
        {
            return pen.CreateObservable(p => p.Width);
        }
    }
}