using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Ellipse : ShapeElementBase
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = LazyGet(element, "cx", 0);
            CenterY = LazyGet(element, "cy", 0);
            RadiusX = LazyGet(element, "rx", Length.Zero);
            RadiusY = LazyGet(element, "ry", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = "ellipse";

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "rx", RadiusX);
            LazySet(element, "ry", RadiusY);

            return element;
        }
    }
}