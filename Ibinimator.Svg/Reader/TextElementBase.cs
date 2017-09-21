using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg.Reader
{
    public abstract class TextElementBase : ShapeElementBase, ITextElement
    {
        public AlignmentBaseline AlignmentBaseline { get; set; }

        public BaselineShift BaselineShift { get; set; }

        public string FontFamily { get; set; }

        public Length FontSize { get; set; } = (12, LengthUnit.Points);

        public FontStretch FontStretch { get; set; } = FontStretch.Inherit;

        public FontWeight FontWeight { get; set; } = FontWeight.Inherit;

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            LazySet(element, "alignment-baseline", AlignmentBaseline.Svgify());
            LazySet(element, "baseline-shift", BaselineShift);
            LazySet(element, "font-family", FontFamily);
            LazySet(element, "font-size", FontSize);
            LazySet(element, "font-stretch", FontStretch);
            LazySet(element, "font-weight", FontWeight);

            return element;
        }
    }
}