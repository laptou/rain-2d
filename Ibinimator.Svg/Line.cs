using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Line : ShapeElement
    {
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X1 = float.Parse((string) element.Attribute("x1") ?? "0");
            Y1 = float.Parse((string) element.Attribute("y1") ?? "0");
            X2 = float.Parse((string) element.Attribute("x2") ?? "0");
            Y2 = float.Parse((string) element.Attribute("y2") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "line";

            LazySet(element, "x1", X1);
            LazySet(element, "y1", Y1);
            LazySet(element, "x2", X2);
            LazySet(element, "y2", Y2);

            return element;
        }
    }
}