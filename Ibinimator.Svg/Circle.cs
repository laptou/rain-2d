using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Circle : ShapeElementBase
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public Length Radius { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = float.Parse((string) element.Attribute("cx") ?? "0");
            CenterY = float.Parse((string) element.Attribute("cy") ?? "0");
            Radius = Length.Parse((string) element.Attribute("r") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "ellipse";

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "r", Radius);

            return element;
        }
    }
}