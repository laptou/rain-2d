using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Circle : ShapeElement
    {
        public Length CenterX { get; set; }
        public Length CenterY { get; set; }
        public Length Radius { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = LazyGet(element, "cx", Length.Zero);
            CenterY = LazyGet(element, "cy", Length.Zero);
            Radius = LazyGet(element, "r", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Circle;

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "r", Radius);

            return element;
        }
    }
}