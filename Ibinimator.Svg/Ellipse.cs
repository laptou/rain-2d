using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.Core.Model;

namespace Ibinimator.Svg
{
    public class Ellipse : ShapeElement
    {
        public Length CenterX { get; set; }
        public Length CenterY { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = LazyGet(element, "cx", Length.Zero);
            CenterY = LazyGet(element, "cy", Length.Zero);
            RadiusX = LazyGet(element, "rx", Length.Zero);
            RadiusY = LazyGet(element, "ry", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Ellipse;

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "rx", RadiusX);
            LazySet(element, "ry", RadiusY);

            return element;
        }
    }
}