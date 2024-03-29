﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model;
using Rain.Formatter.Svg.Paint;
using Rain.Formatter.Svg.Shapes;
using Rain.Formatter.Svg.Structure;

using Group = Rain.Formatter.Svg.Structure.Group;

namespace Rain.Formatter.Svg.Utilities
{
    internal static class X
    {
        private static readonly Regex TransformSyntax = new Regex(
            @"(?:(matrix)\s*\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*)(?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)\)|(translate)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)?\)|(scale)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)?\)|(rotate)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)(?:(?:,\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*){2})?\)|(skewX)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*)|(skewY)\((?:\s*((?:[+-]?\d+|[+-]?\d*\.\d+)(?:[Ee][+-]?\d+)?)\s*))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IElement FromXml(XElement element, SvgContext context)
        {
            if (element == null) return null;

            IElement ielement;

            switch (element.Name.LocalName)
            {
                case "use":
                    ielement = new Use();

                    break;

                case "circle":
                    ielement = new Circle();

                    break;
                case "ellipse":
                    ielement = new Ellipse();

                    break;
                case "g":
                    ielement = new Group();

                    break;
                case "line":
                    ielement = new Line();

                    break;
                case "path":
                    ielement = new Path();

                    break;
                case "polygon":
                    ielement = new Polygon();

                    break;
                case "polyline":
                    ielement = new Polyline();

                    break;
                case "rect":
                    ielement = new Rectangle();

                    break;
                case "text":
                    ielement = new Text();

                    break;
                case "defs":
                    ielement = new Defs();

                    break;
                case "solidColor":
                    ielement = new SolidColorPaint();

                    break;
                case "linearGradient":
                    ielement = new LinearGradientPaint();

                    break;
                case "radialGradient":
                    ielement = new RadialGradientPaint();

                    break;
                default:

                    return null;
            }

            ielement.FromXml(element, context);

            return ielement;
        }

        public static Matrix3x2 ParseTransform(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                var transformMatches = TransformSyntax.Matches(str);
                var transform = Matrix3x2.Identity;

                foreach (Match transformMatch in transformMatches)
                {
                    var groups = transformMatch.Groups.OfType<System.Text.RegularExpressions.Group>()
                                               .Skip(1)
                                               .SelectMany(g => g.Captures.OfType<Capture>().Select(c => c.Value))
                                               .Where(g => !string.IsNullOrWhiteSpace(g))
                                               .ToArray();

                    switch (groups[0])
                    {
                        case "matrix":
                            transform = new Matrix3x2(float.Parse(groups[1]),
                                                      float.Parse(groups[2]),
                                                      float.Parse(groups[3]),
                                                      float.Parse(groups[4]),
                                                      float.Parse(groups[5]),
                                                      float.Parse(groups[6])) * transform;

                            break;
                        case "scale":
                            if (groups.Length == 2)
                                transform = Matrix3x2.CreateScale(float.Parse(groups[1])) * transform;

                            if (groups.Length == 3)
                                transform = Matrix3x2.CreateScale(float.Parse(groups[1]), float.Parse(groups[2])) *
                                            transform;

                            break;

                        case "translate":
                            if (groups.Length == 2)
                                transform =
                                    Matrix3x2.CreateTranslation(float.Parse(groups[1]), float.Parse(groups[1])) *
                                    transform;

                            if (groups.Length == 3)
                                transform =
                                    Matrix3x2.CreateTranslation(float.Parse(groups[1]), float.Parse(groups[2])) *
                                    transform;

                            break;

                        case "rotate":
                            if (groups.Length == 2)
                                transform = Matrix3x2.CreateRotation(float.Parse(groups[1]) / 180 * (float) Math.PI) *
                                            transform;

                            if (groups.Length == 4)
                                transform = Matrix3x2.CreateRotation(float.Parse(groups[1]) / 180 * (float) Math.PI,
                                                                     new Vector2(
                                                                         float.Parse(groups[2]),
                                                                         float.Parse(groups[3]))) * transform;

                            break;

                        case "skewX":
                            transform = Matrix3x2.CreateSkew(float.Parse(groups[1]) / 180 * (float) Math.PI, 0) *
                                        transform;

                            break;

                        case "skewY":
                            transform = Matrix3x2.CreateSkew(0, float.Parse(groups[1]) / 180 * (float) Math.PI) *
                                        transform;

                            break;
                    }
                }

                return transform;
            }

            return Matrix3x2.Identity;
        }

        public static string Svgify(this Enum enumeration)
        {
            var name = enumeration.ToString();
            var words = new List<string>();
            var current = "";

            foreach (var character in name)
            {
                if (char.IsUpper(character) &&
                    current != "")
                {
                    words.Add(current.ToLower());
                    current = "";
                }

                current += character;
            }

            words.Add(current.ToLower());

            return string.Join("-", words);
        }

        public static string Svgify(this RectangleF rect)
        {
            return $"rect({rect.Left}px, {rect.Top}px, {rect.Right}px, {rect.Bottom} px";
        }
    }
}