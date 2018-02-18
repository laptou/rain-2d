using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Svg.Structure;

namespace Ibinimator.Svg.Shapes
{
    public abstract class ShapeElement : GraphicalElement, IShapeElement
    {
        #region IShapeElement Members

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (Paint.Paint.TryParse(LazyGet(element, "fill", true), out var fill))
                Fill = fill;

            if (float.TryParse(LazyGet(element, "fill-opacity", true), out var fillOpacity))
                FillOpacity = fillOpacity;

            if (Paint.Paint.TryParse(LazyGet(element, "stroke", true), out var stroke))
                Stroke = stroke;

            if (Length.TryParse(LazyGet(element, "stroke-width", true), out var strokeWidth))
                StrokeWidth = strokeWidth;

            if (float.TryParse(LazyGet(element, "stroke-opacity", true), out var strokeOpacity))
                StrokeOpacity = strokeOpacity;

            StrokeDashArray = LazyGet<float[]>(element, "stroke-dasharray", inherit: true);
            StrokeDashOffset = LazyGet(element, "stroke-dashoffset", 0f, true);

            StrokeLineCap = LazyGet(element, "stroke-linecap", LineCap.Butt, true);
            StrokeLineJoin = LazyGet(element, "stroke-linejoin", LineJoin.Miter, true);
            StrokeMiterLimit = LazyGet(element, "stroke-miterlimit", 4f, true);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            if (Fill != null)
            {
                LazySet(element, "fill", Fill.ToInline());
                LazySet(element, "fill-opacity", FillOpacity * Fill.Opacity, 1);
                LazySet(element, "fill-rule", FillRule);
            }

            if (Stroke != null)
            {
                LazySet(element, "stroke", Stroke.ToInline());
                LazySet(element, "stroke-dasharray", StrokeDashArray);
                LazySet(element, "stroke-dash-offset", StrokeDashOffset);
                LazySet(element, "stroke-opacity", StrokeOpacity * Stroke.Opacity, 1);
                LazySet(element, "stroke-linecap", StrokeLineCap);
                LazySet(element, "stroke-linejoin", StrokeLineJoin);
                LazySet(element, "stroke-miterlimit", StrokeMiterLimit, 4f);
                LazySet(element, "stroke-width", StrokeWidth, new Length(1, LengthUnit.Pixels));
            }

            return element;
        }

        public Paint.Paint Fill { get; set; }

        public float FillOpacity { get; set; } = 1;

        public FillRule FillRule { get; set; }

        public Paint.Paint Stroke { get; set; }

        public float[] StrokeDashArray { get; set; } = new float[0];

        public float StrokeDashOffset { get; set; }

        public LineCap StrokeLineCap { get; set; } = LineCap.Butt;

        public LineJoin StrokeLineJoin { get; set; } = LineJoin.Miter;

        public float StrokeMiterLimit { get; set; } = 4;

        public float StrokeOpacity { get; set; } = 1;

        public Length StrokeWidth { get; set; } = (1, LengthUnit.Pixels);

        #endregion
    }
}