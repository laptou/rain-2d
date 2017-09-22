using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Rectangle : ShapeElementBase
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }
        public Length Width { get; set; }
        public Length Height { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X = float.Parse((string)element.Attribute("x") ?? "0");
            Y = float.Parse((string)element.Attribute("y") ?? "0");
            RadiusX = Length.Parse((string)element.Attribute("rx") ?? "0");
            RadiusY = Length.Parse((string)element.Attribute("ry") ?? "0");
            Width = Length.Parse((string)element.Attribute("width") ?? "0");
            Height = Length.Parse((string)element.Attribute("height") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "rect";

            LazySet(element, "x", X);
            LazySet(element, "y", Y);
            LazySet(element, "rx", RadiusX);
            LazySet(element, "ry", RadiusY);
            LazySet(element, "width", Width);
            LazySet(element, "height", Height);

            return element;
        }
    }
}