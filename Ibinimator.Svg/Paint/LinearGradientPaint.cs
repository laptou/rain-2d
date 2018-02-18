using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Core.Model.Measurement;

namespace Ibinimator.Svg.Paint
{
    public class LinearGradientPaint : GradientPaint
    {
        public LinearGradientPaint() { }

        public LinearGradientPaint(string id, IEnumerable<GradientStop> stops)
        {
            Id = id;
            Stops = stops.ToArray();
        }

        public Length X1 { get; set; }
        public Length X2 { get; set; } = (100, LengthUnit.Percent);
        public Length Y1 { get; set; }
        public Length Y2 { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X1 = LazyGet(element, "x1", Length.Zero);
            Y1 = LazyGet(element, "y1", Length.Zero);
            X2 = LazyGet(element, "x2", Length.Zero);
            Y2 = LazyGet(element, "y2", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.LinearGradient;

            LazySet(element, "x1", X1);
            LazySet(element, "y1", Y1);
            LazySet(element, "x2", X2);
            LazySet(element, "y2", Y2);

            return element;
        }
    }
}