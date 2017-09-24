using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class GraphicalElement : ElementBase, IGraphicalElement
    {
        private static readonly Regex TransformSyntax = new Regex(@"(?:(matrix)\s*\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)\)|(translate)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)?\)|(scale)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)?\)|(rotate)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*){2})?\)|(skewX)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)|(skewY)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            // LazyGet(element, "clip", RectangleF.Empty);
            //LazyGet(element, "clip-path", ClipPath);
            //LazyGet(element, "clip-rule", ClipRule);
            ColorInterpolation = LazyGet<ColorInterpolation>(element, "color-interpolation");
            ColorFilterInterpolation = LazyGet<ColorInterpolation>(element, "filter-color-interpolation");
            //LazyGet(element, "color", Color);
            //Cursor = LazyGet<Cursor>(element, "cursor", Cursor);
            Direction = LazyGet<Direction>(element, "direction");
            //LazyGet(element, "filter", Filter);
            Kerning = LazyGet(element, "kerning", Length.Zero);
            LetterSpacing = LazyGet(element, "letter-spacing", Length.Zero);
            //LazyGet(element, "mask", Mask);
            Opacity = LazyGet(element, "opacity", 1);

            var transformStr = (string)element.Attribute("transform");

            if (!string.IsNullOrWhiteSpace(transformStr))
            {
                var transformMatches = TransformSyntax.Matches(transformStr);
                var transform = Matrix3x2.Identity;

                foreach (Match transformMatch in transformMatches)
                {
                    var groups = transformMatch.Groups
                        .OfType<System.Text.RegularExpressions.Group>()
                        .Skip(1)
                        .SelectMany(g => g.Captures.OfType<Capture>().Select(c => c.Value))
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .ToArray();

                    switch (groups[0])
                    {
                        case "matrix":
                            transform = new Matrix3x2(
                                float.Parse(groups[1]),
                                float.Parse(groups[2]),
                                float.Parse(groups[3]),
                                float.Parse(groups[4]),
                                float.Parse(groups[5]),
                                float.Parse(groups[6])) * transform;
                            break;
                        case "scale":
                            if(groups.Length == 2)
                                transform = 
                                    Matrix3x2.CreateScale(
                                        float.Parse(groups[1])) * transform;

                            if (groups.Length == 3)
                                transform =
                                    Matrix3x2.CreateScale(
                                        float.Parse(groups[1]),
                                        float.Parse(groups[2])) * transform;
                            break;

                        case "translate":
                            if (groups.Length == 2)
                                transform =
                                    Matrix3x2.CreateTranslation(
                                        float.Parse(groups[1]), 
                                        float.Parse(groups[1])) * transform;

                            if (groups.Length == 3)
                                transform =
                                    Matrix3x2.CreateTranslation(
                                        float.Parse(groups[1]),
                                        float.Parse(groups[2])) * transform;
                            break;

                        case "rotate":
                            if (groups.Length == 2)
                                transform =
                                    Matrix3x2.CreateRotation(
                                        float.Parse(groups[1]) / 180 * (float)Math.PI) * transform;

                            if (groups.Length == 4)
                                transform =
                                    Matrix3x2.CreateRotation(
                                        float.Parse(groups[1]) / 180 * (float)Math.PI,
                                        new Vector2(
                                            float.Parse(groups[2]),
                                            float.Parse(groups[3]))) * transform;
                            break;

                        case "skewX":
                            transform =
                                Matrix3x2.CreateSkew(
                                    float.Parse(groups[1]) / 180 * (float)Math.PI,
                                    0) * transform;
                            break;

                        case "skewY":
                            transform =
                                Matrix3x2.CreateSkew(
                                    0,
                                    float.Parse(groups[1]) / 180 * (float)Math.PI) * transform;
                            break;
                    }
                }

                Transform = transform;
            }
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

            if(!Transform.IsIdentity)
                LazySet(element, "transform", $"matrix({Transform.M11},{Transform.M12},{Transform.M21},{Transform.M22},{Transform.M31},{Transform.M32})");

            return element;
        }
    }
}