using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.Core.Model;

namespace Ibinimator.Svg
{
    public abstract class ShapeElement : GraphicalElement, IShapeElement
    {
        #region IShapeElement Members

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (Paint.TryParse(LazyGet(element, "fill", true), out var fill))
                Fill = fill.Resolve(context);

            if(float.TryParse(LazyGet(element, "fill-opacity", true), out var fillOpacity))
                FillOpacity = fillOpacity;

            if (Paint.TryParse(LazyGet(element, "stroke", true), out var stroke))
                Stroke = stroke.Resolve(context);

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

            LazySet(element, "fill", Fill);
            LazySet(element, "fill-opacity", FillOpacity, 1);
            LazySet(element, "fill-rule", FillRule);
            LazySet(element, "stroke", Stroke);
            LazySet(element, "stroke-dasharray", StrokeDashArray);
            LazySet(element, "stroke-dash-offset", StrokeDashOffset);
            LazySet(element, "stroke-opacity", StrokeOpacity, 1);
            LazySet(element, "stroke-linecap", StrokeLineCap);
            LazySet(element, "stroke-linejoin", StrokeLineJoin);
            LazySet(element, "stroke-miterlimit", StrokeMiterLimit);
            LazySet(element, "stroke-width", StrokeWidth, new Length(1, LengthUnit.Pixels));

            return element;
        }

        public Paint Fill { get; set; }

        public float FillOpacity { get; set; } = 1;

        public FillRule FillRule { get; set; }

        public Paint Stroke { get; set; }

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