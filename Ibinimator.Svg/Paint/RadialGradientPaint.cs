using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Core.Model.Measurement;

namespace Ibinimator.Svg.Paint
{
    public class RadialGradientPaint : GradientPaint
    {
        public Length CenterX { get; set; }
        public Length CenterY { get; set; }
        public Length FocusX { get; set; }
        public Length FocusY { get; set; }
        public Length Radius { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = LazyGet(element, "cx", Length.Zero);
            CenterY = LazyGet(element, "cy", Length.Zero);
            CenterY = LazyGet(element, "fx", Length.Zero);
            CenterY = LazyGet(element, "fy", Length.Zero);
            Radius = LazyGet(element, "r", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.RadialGradient;

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "fx", FocusX, CenterX);
            LazySet(element, "fy", FocusY, CenterY);
            LazySet(element, "r", Radius);

            return element;
        }
    }
}