using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Formatter.Svg.Shapes
{
    public class Polygon : ShapeElement
    {
        private static readonly Regex PointsSyntax =
            new Regex(
                @"\s*(?:,?(\s*(?:[-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:[Ee][-+]?[0-9]+)?)\s*))",
                RegexOptions.Compiled);

        public Vector2[] Points { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            var strPoints = (string) element.Attribute("points") ?? "";

            var coords = PointsSyntax.Matches(strPoints)
                                     .OfType<Match>()
                                     .Select(m => float.Parse(m.Groups[1].Value))
                                     .ToArray();

            Points = new Vector2[coords.Length / 2];

            for (var i = 0; i < coords.Length; i += 2)
                Points[i / 2] = new Vector2(coords[i], coords[i + 1]);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Polygon;

            LazySet(element, "points", string.Join(" ", Points));

            return element;
        }
    }
}