using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Formatter.Svg.Enums;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Structure
{
    public abstract class GraphicalElementBase : ElementBase, IGraphicalElement
    {
        #region IGraphicalElement Members

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            // LazyGet(element, "clip", RectangleF.Empty);
            //LazyGet(element, "clip-path", ClipPath);
            //LazyGet(element, "clip-rule", ClipRule);
            ColorInterpolation = LazyGet<ColorInterpolation>(element, "color-interpolation");
            ColorFilterInterpolation =
                LazyGet<ColorInterpolation>(element, "filter-color-interpolation");

            //LazyGet(element, "color", Color);
            //Cursor = LazyGet<Cursor>(element, "cursor", Cursor);
            Direction = LazyGet<Direction>(element, "direction");

            //LazyGet(element, "filter", Filter);
            Kerning = LazyGet(element, "kerning", Length.Zero);
            LetterSpacing = LazyGet(element, "letter-spacing", Length.Zero);

            //LazyGet(element, "mask", Mask);
            Opacity = LazyGet(element, "opacity", 1);

            Transform = X.ParseTransform(LazyGet(element, "transform"));
        }

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

            if (!Transform.IsIdentity)
                LazySet(element,
                        "transform",
                        @"matrix(" + $"{Transform.M11},{Transform.M12}," +
                        $"{Transform.M21},{Transform.M22}," + $"{Transform.M31},{Transform.M32})");

            return element;
        }

        public RectangleF? Clip { get; set; }

        public Uri ClipPath { get; set; }

        public FillRule ClipRule { get; set; }

        public Color Color { get; set; }

        public ColorInterpolation ColorFilterInterpolation { get; set; }

        public ColorInterpolation ColorInterpolation { get; set; }

        public Cursor Cursor { get; set; }

        public Direction Direction { get; set; }

        public Uri Filter { get; set; }

        public Length? Kerning { get; set; }

        public Length? LetterSpacing { get; set; }

        public Uri Mask { get; set; }

        public float Opacity { get; set; } = 1;

        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        #endregion
    }
}