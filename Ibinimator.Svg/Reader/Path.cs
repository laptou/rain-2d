using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg.Reader
{
    public class Path : ShapeElementBase
    {
        public PathNode[] Data { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Data = PathDataParser.Parse((string) element.Attribute("points") ?? "").ToArray();
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "path";

            if (Data.Length <= 0) return element;

            var begin = true;
            var pathData = "";

            foreach (var pathNode in Data)
            {
                if (begin)
                {
                    pathData += $" M {pathNode.X},{pathNode.Y}";
                    begin = false;
                    continue;
                }

                switch (pathNode)
                {
                    case CloseNode c:
                        pathData += $" Z";
                        begin = true;
                        break;
                    case QuadraticPathNode qn:
                        pathData += $" Q {qn.Control.X},{qn.Control.Y}" +
                                    $" {qn.X},{qn.Y}";
                        break;
                    case CubicPathNode cn:
                        pathData += $" C {cn.Control1.X},{cn.Control1.Y}" +
                                    $" {cn.Control2.X},{cn.Control2.Y}" +
                                    $" {cn.X},{cn.Y}";
                        break;
                    default:
                        pathData += $" L {pathNode.X},{pathNode.Y}";
                        break;
                }
            }

            element.SetAttributeValue("d", pathData);

            return element;
        }
    }

    public class Polyline : ShapeElementBase
    {
        private static readonly Regex PointsSyntax =
            new Regex(@"\s*(?:,?(\s*(?:[-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:[Ee][-+]?[0-9]+)?)\s*))",
                RegexOptions.Compiled);

        public Vector2[] Points { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            var strPoints = (string)element.Attribute("points") ?? "";

            var coords =
                PointsSyntax.Matches(strPoints)
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
            element.Name = "polyline";

            LazySet(element, "points", string.Join(" ", Points));

            return element;
        }
    }

    public class Polygon : ShapeElementBase
    {
        private static readonly Regex PointsSyntax =
            new Regex(@"\s*(?:,?(\s*(?:[-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:[Ee][-+]?[0-9]+)?)\s*))",
                RegexOptions.Compiled);

        public Vector2[] Points { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            var strPoints = (string) element.Attribute("points") ?? "";

            var coords =
                    PointsSyntax.Matches(strPoints)
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
            element.Name = "polygon";

            LazySet(element, "points", string.Join(" ", Points));

            return element;
        }
    }

    public class Line : ShapeElementBase
    {
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X1 = float.Parse((string) element.Attribute("x1") ?? "0");
            Y1 = float.Parse((string) element.Attribute("y1") ?? "0");
            X2 = float.Parse((string) element.Attribute("x2") ?? "0");
            Y2 = float.Parse((string) element.Attribute("y2") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "line";

            LazySet(element, "x1", X1);
            LazySet(element, "y1", Y1);
            LazySet(element, "x2", X2);
            LazySet(element, "y2", Y2);

            return element;
        }
    }

    public class Rectangle : ShapeElementBase
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }
        public Length Width { get; set; }
        public Length Height { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X = float.Parse((string)element.Attribute("x") ?? "0");
            Y = float.Parse((string)element.Attribute("x") ?? "0");
            RadiusX = Length.Parse((string)element.Attribute("rx") ?? "0");
            RadiusY = Length.Parse((string)element.Attribute("ry") ?? "0");
            Width = Length.Parse((string)element.Attribute("width") ?? "0");
            Height = Length.Parse((string)element.Attribute("height") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "rect";

            LazySet(element, "x", X);
            LazySet(element, "y", Y);
            LazySet(element, "rx", RadiusX);
            LazySet(element, "ry", RadiusY);
            LazySet(element, "width", Width);
            LazySet(element, "height", Height);

            return element;
        }
    }

    public class Ellipse : ShapeElementBase
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public Length RadiusX { get; set; }
        public Length RadiusY { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = float.Parse((string)element.Attribute("cx") ?? "0");
            CenterY = float.Parse((string)element.Attribute("cy") ?? "0");
            RadiusX = Length.Parse((string)element.Attribute("rx") ?? "0");
            RadiusY = Length.Parse((string)element.Attribute("ry") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "ellipse";

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "rx", RadiusX);
            LazySet(element, "ry", RadiusY);

            return element;
        }
    }

    public class Circle : ShapeElementBase
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public Length Radius { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = float.Parse((string)element.Attribute("cx") ?? "0");
            CenterY = float.Parse((string)element.Attribute("cy") ?? "0");
            Radius = Length.Parse((string)element.Attribute("r") ?? "0");
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "ellipse";

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "r", Radius);

            return element;
        }
    }
}