using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Rectangle : ShapeElementBase
    {
        public Length Height { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }
        public Length Width { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X = LazyGet(element, "x", 0);
            Y = LazyGet(element, "y", 0);
            RadiusX = LazyGet(element, "rx", Length.Zero);
            RadiusY = LazyGet(element, "ry", Length.Zero);
            Width = LazyGet(element, "width", Length.Zero);
            Height = LazyGet(element, "height", Length.Zero);
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