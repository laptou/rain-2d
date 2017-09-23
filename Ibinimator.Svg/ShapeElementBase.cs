using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class ShapeElementBase : GraphicalElementBase, IShapeElement
    {
        #region IShapeElement Members

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (element.Attribute("fill") != null)
                Fill = Paint.Parse((string) element.Attribute("fill"));

            if (element.Attribute("fill-opacity") != null)
                FillOpacity = float.Parse((string) element.Attribute("fill-opacity"));

            if (element.Attribute("stroke") != null)
                Stroke = Paint.Parse((string) element.Attribute("stroke"));

            if (element.Attribute("stroke-width") != null)
                StrokeWidth = Length.Parse((string) element.Attribute("stroke-width"));

            if (element.Attribute("stroke-opacity") != null)
                StrokeOpacity = float.Parse((string) element.Attribute("stroke-opacity"));
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

        public Paint? Fill { get; set; }

        public float FillOpacity { get; set; } = 1;

        public FillRule FillRule { get; set; }

        public Paint? Stroke { get; set; }

        public float[] StrokeDashArray { get; set; } = new float[0];

        public float StrokeDashOffset { get; set; }

        // TODO: ‘stroke-linecap’, ‘stroke-linejoin’, ‘stroke-miterlimit’

        public float StrokeOpacity { get; set; } = 1;

        public Length StrokeWidth { get; set; }

        #endregion
    }
}