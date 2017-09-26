using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class ShapeElement : GraphicalElement, IShapeElement
    {
        #region IShapeElement Members

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (Paint.TryParse(LazyGet(element, "fill", true), out var fill))
                Fill = fill;

            if(float.TryParse(LazyGet(element, "fill-opacity", true), out var fillOpacity))
                FillOpacity = fillOpacity;

            if (Paint.TryParse(LazyGet(element, "stroke", true), out var stroke))
                Stroke = stroke;

            if (Length.TryParse(LazyGet(element, "stroke-width", true), out var strokeWidth))
                StrokeWidth = strokeWidth;

            if (float.TryParse(LazyGet(element, "stroke-opacity", true), out var strokeOpacity))
                StrokeOpacity = strokeOpacity;

            StrokeDashArray = LazyGet<float[]>(element, "stroke-dasharray", true);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            LazySet(element, "fill", Fill);
            LazySet(element, "fill-opacity", FillOpacity);
            LazySet(element, "fill-rule", FillRule);
            LazySet(element, "stroke", Stroke);
            LazySet(element, "stroke-dasharray", StrokeDashArray);
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

        public Length StrokeWidth { get; set; } = (1, LengthUnit.Pixels);

        #endregion
    }
}