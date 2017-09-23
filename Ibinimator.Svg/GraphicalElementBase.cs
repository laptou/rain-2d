using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class GraphicalElementBase : ElementBase
    {
        public RectangleF? Clip { get; set; }

        public Iri? ClipPath { get; set; }

        public FillRule ClipRule { get; set; }

        public Color Color { get; set; }

        public ColorInterpolation ColorFilterInterpolation { get; set; }

        public ColorInterpolation ColorInterpolation { get; set; }

        public Cursor Cursor { get; set; }

        public Direction Direction { get; set; }

        public Iri? Filter { get; set; }

        public Length? Kerning { get; set; }

        public Length? LetterSpacing { get; set; }

        public Iri? Mask { get; set; }

        public float Opacity { get; set; } = 1;

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            LazySet(element, "clip", Clip?.Svgify());
            LazySet(element, "clip-path", ClipPath);
            LazySet(element, "clip-rule", ClipRule);
            LazySet(element, "color-interpolation", ColorInterpolation);
            LazySet(element, "filter-color-interpolation", ColorFilterInterpolation);
            LazySet(element, "color", Color);
            LazySet(element, "cursor", Cursor);
            LazySet(element, "direction", Direction);
            LazySet(element, "filter", Filter);
            LazySet(element, "kerning", Kerning);
            LazySet(element, "letter-spacing", LetterSpacing);
            LazySet(element, "mask", Mask);
            LazySet(element, "opacity", Opacity, 1);

            return element;
        }
    }
}