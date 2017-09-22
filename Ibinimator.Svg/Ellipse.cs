using Ibinimator.Core.Mathematics;
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

            CenterX = float.Parse((string)element.Attribute("cx") ?? "0");
            CenterY = float.Parse((string)element.Attribute("cy") ?? "0");
            RadiusX = Length.Parse((string)element.Attribute("rx") ?? "0");
            RadiusY = Length.Parse((string)element.Attribute("ry") ?? "0");
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