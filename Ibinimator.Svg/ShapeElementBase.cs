using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class ShapeElementBase : GraphicalElementBase, IShapeElement
    {
        public Paint? Fill { get; set; }

        public float FillOpacity { get; set; } = 1;

        public FillRule FillRule { get; set; }

        public Paint? Stroke { get; set; }

        public float[] StrokeDashArray { get; set; } = new float[0];

        public float StrokeDashOffset { get; set; }

        // TODO: ‘stroke-linecap’, ‘stroke-linejoin’, ‘stroke-miterlimit’

        public float StrokeOpacity { get; set; } = 1;

        public float StrokeWidth { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (element.Attribute("fill") != null)
                Fill = Paint.Parse((string) element.Attribute("fill"));
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            LazySet(element, "fill", Fill);
            LazySet(element, "fill-opacity", FillOpacity);
            LazySet(element, "fill-rule", FillRule);
            LazySet(element, "stroke", Stroke);
            LazySet(element, "stroke-dash-array", StrokeDashArray);
            LazySet(element, "stroke-dash-offset", StrokeDashOffset);
            LazySet(element, "stroke-opacity", StrokeOpacity);
            LazySet(element, "stroke-width", StrokeWidth);

            return element;
        }
    }
}