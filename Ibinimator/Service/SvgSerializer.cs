using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Ibinimator.Model;
using Ibinimator.Shared;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.View;
using SharpDX;
using SharpDX.Direct2D1;
using Ellipse = Ibinimator.Model.Ellipse;
using Group = Ibinimator.Model.Group;
using Layer = Ibinimator.Model.Layer;
using Path = Ibinimator.Model.Path;
using Rectangle = Ibinimator.Model.Rectangle;

namespace Ibinimator.Service
{
    public interface ISvgSerializable
    {
        XElement GetElement();
    }

    public static class SvgNames
    {
        public static readonly XNamespace Namespace = "http://www.w3.org/2000/svg";
        public static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";

        public static readonly XName Svg = Namespace + "svg";
        public static readonly XName Defs = Namespace + "defs";
        public static readonly XName Rect = Namespace + "rect";
        public static readonly XName Ellipse = Namespace + "ellipse";
        public static readonly XName Circle = Namespace + "circle";
        public static readonly XName Path = Namespace + "path";
        public static readonly XName Polygon = Namespace + "polygon";
        public static readonly XName Polyline = Namespace + "polyline";
        public static readonly XName Group = Namespace + "g";
        public static readonly XName[] Visuals = {Rect, Ellipse, Circle, Path, Polygon, Polyline, Group};

        public static readonly XName SolidColor = Namespace + "solidColor";
        public static readonly XName LinearGradient = Namespace + "linearGradient";
        public static readonly XName RadialGradient = Namespace + "radialGradient";
        public static readonly XName Stop = Namespace + "stop";
    }

    public static class SvgSerializer
    {
        public static Document DeserializeDocument(XDocument xdoc)
        {
            return Parse<Document>(xdoc.Root, null, xdoc.Root);
        }

        public static XDocument SerializeDocument(Document doc)
        {
            var ns = XNamespace.Get("http://www.w3.org/2000/svg");
            var root = new XElement(SvgNames.Svg);

            root.Add(new XAttribute("version", "2.0"));

            var defs = new XElement(SvgNames.Defs);

            foreach (var brush in doc.Swatches)
                defs.Add(brush.GetElement());

            root.Add(defs);

            foreach (var layer in doc.Root.SubLayers)
                root.Add(layer.GetElement());

            return new XDocument(root);
        }

        public static string ToCss(this Color4 color)
        {
            return "rgb(" +
                   $"{color.Red * 100}%," +
                   $"{color.Green * 100}%," +
                   $"{color.Blue * 100}%" +
                   ")";
        }

        public static string ToCss(this Matrix3x2 matrix)
        {
            return
                "matrix(" +
                $"{matrix.M11},{matrix.M12}," +
                $"{matrix.M21},{matrix.M22}," +
                $"{matrix.M31},{matrix.M32}" +
                ")";
        }

        private static float[] Extract(string data, UnitType type = UnitType.None, params float[] maximums)
        {
            if (string.IsNullOrWhiteSpace(data)) return new float[] {0};
            var function = Regex.Match(data, @"(\w+)\([\w\d,.%-]+\)").Groups[1].Value.ToLowerInvariant();
            var matches = Regex.Matches(data,
                @"([-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:E[-+]?[0-9]+)?)\s*(%|px|pt|in|mm|cm|ms|s|f|m|h|deg|\u00B0)?");

            var counter = 0;
            var inputs =
                from Match match in matches
                select (index: counter++,
                value: float.Parse(match.Groups[1].Value),
                unit: match.Groups[2].Value);
            var values = new float[matches.Count];

            foreach (var input in inputs)
            {
                var value = input.value;

                if (input.unit == "%")
                {
                    if (maximums.Length == 1) value = value / 100f * maximums[0];
                    else value /= 100f * maximums[input.index];
                }
                else
                {
                    var unit = UnitConverter.GetUnit(input.unit, type);
                    value *= UnitConverter.ConversionFactor(unit, type.GetBaseUnit());
                }

                values[input.index] = value;
            }

            return values;
        }

        private static T Parse<T>(XElement element, Document doc, XContainer root) where T : class
        {
            var style = element.Attribute("style")?.Value;
            if (style != null)
            {
                var rules =
                    from Match match in Regex.Matches(style, @"([a-z-]+):([a-zA-Z0-9""'#\(\)\.,]+);?")
                    select (name: match.Groups[1].Value, value: match.Groups[2].Value);

                foreach (var rule in rules)
                    element.SetAttributeValue(rule.name, rule.value);
            }

            var href = element.Attribute(SvgNames.XLink + "href")?.Value;
            
            if (element.Name == SvgNames.Svg)
            {
                var document = new Document();
                var defs = Parse<object[]>(element.Element(SvgNames.Defs), document, root);

                document.Swatches = new ObservableCollection<BrushInfo>(defs.OfType<BrushInfo>());
                document.Root = new Group();

                foreach (var layerElement in
                    element.Elements().Where(xe => SvgNames.Visuals.Contains(xe.Name)))
                    document.Root.Add(Parse<Layer>(layerElement, document, root));

                return document as T;
            }

            #region Defs

            if (element.Name == SvgNames.Defs)
            {
                return element.Elements().Select(def => Parse<object>(def, doc, root)).ToArray() as T;
            }

            if (element.Name == SvgNames.SolidColor)
            {
                var info = new SolidColorBrushInfo();
                info.Color = ReadColor(element, "solid-color");

                if (element.Attribute("solid-opacity") != null)
                    info.Opacity = ReadFloat(element, "solid-opacity");

                return info as T;
            }

            if (element.Name == SvgNames.LinearGradient || element.Name == SvgNames.RadialGradient)
            {
                var info = new GradientBrushInfo();

                if (href != null)
                    info = Resolve(root, href) as GradientBrushInfo ?? info;

                info.Name = element.Attribute("id")?.Value;

                info.StartPoint = new Vector2(ReadFloat(element, "x1"), ReadFloat(element, "y1"));
                info.EndPoint = new Vector2(ReadFloat(element, "x2"), ReadFloat(element, "y2"));

                if (element.Attribute("opacity") != null)
                    info.Opacity = ReadFloat(element, "opacity");

                foreach (var stop in element.Elements(SvgNames.Stop))
                    info.Stops.Add((GradientStop)Parse<object>(stop, doc, root));

                return info as T;
            }

            if (element.Name == SvgNames.Stop)
            {
                var stop = new GradientStop();
                var color = ReadColor(element, "stop-color");
                color.Alpha = ReadFloat(element, "stop-opacity", 1);
                stop.Color = color;
                stop.Position = ReadFloat(element, "offset", 1);
                return stop as T;
            }

            #endregion

            #region Visuals

            if (SvgNames.Visuals.Contains(element.Name))
            {
                Layer layer;

                if (element.Name == SvgNames.Rect)
                {
                    var rect = new Rectangle();

                    rect.Height = ReadFloat(element, "height");
                    rect.Width = ReadFloat(element, "width");
                    rect.Position = new Vector2(ReadFloat(element, "x"), ReadFloat(element, "y"));

                    layer = rect;
                }
                else if (element.Name == SvgNames.Ellipse)
                {
                    var ellipse = new Ellipse();

                    ellipse.RadiusX = ReadFloat(element, "rx");
                    ellipse.RadiusY = ReadFloat(element, "ry");
                    ellipse.Position =
                        new Vector2(
                            ReadFloat(element, "cx") - ellipse.RadiusX,
                            ReadFloat(element, "cy") - ellipse.RadiusY
                        );

                    layer = ellipse;
                }
                else if (element.Name == SvgNames.Circle)
                {
                    var ellipse = new Ellipse();

                    ellipse.RadiusX = ReadFloat(element, "r");
                    ellipse.RadiusY = ellipse.RadiusX;
                    ellipse.Position =
                        new Vector2(
                            ReadFloat(element, "cx") - ellipse.RadiusX,
                            ReadFloat(element, "cy") - ellipse.RadiusY
                        );

                    layer = ellipse;
                }
                else if (element.Name == SvgNames.Path)
                {
                    var path = new Path();
                    var command = element.Attribute("d")?.Value;
                    path.Nodes = new ObservableCollection<PathNode>(PathDataSerializer.Parse(command));
                    layer = path;
                }
                else if (element.Name == SvgNames.Polygon || element.Name == SvgNames.Polyline)
                {
                    var path = new Path();
                    var points =
                        from point in (element.Attribute("points")?.Value ?? "").Split(' ')
                        let xy = point.Split(',').Select(float.Parse).ToArray()
                        select new Vector2(xy[0], xy[1]);

                    foreach (var point in points)
                        path.Nodes.Add(new PathNode {X = point.X, Y = point.Y});

                    path.Closed = element.Name == SvgNames.Polygon;
                    layer = path;
                }
                else if (element.Name == SvgNames.Group)
                {
                    var group = new Group();

                    foreach (var layerElement in element.Elements())
                        group.Add(Parse<Layer>(layerElement, doc, root));

                    layer = group;
                }
                else
                {
                    throw new InvalidDataException();
                }

                if (layer is Shape shape)
                {
                    var fillValue = element.Attribute("fill")?.Value;

                    if (!string.IsNullOrWhiteSpace(fillValue))
                    {
                        if (fillValue.StartsWith("url"))
                        {
                            var id = fillValue.Replace("url(#", "").Replace(")", "");
                            shape.FillBrush = doc.Swatches.FirstOrDefault(s => s.Name == id);
                        }
                        else
                        {
                            shape.FillBrush = new SolidColorBrushInfo
                            {
                                Color = ReadColor(element, "fill"),
                                Opacity = ReadFloat(element, "fill-opacity", 1)
                            };
                        }
                    }

                    var strokeValue = element.Attribute("stroke")?.Value;

                    if (!string.IsNullOrWhiteSpace(strokeValue))
                    {
                        if (strokeValue.StartsWith("url"))
                        {
                            var id = strokeValue.Replace("url(#", "").Replace(")", "");
                            shape.StrokeBrush = doc.Swatches.FirstOrDefault(s => s.Name == id);
                        }
                        else
                        {
                            shape.StrokeBrush = new SolidColorBrushInfo
                            {
                                Color = ReadColor(element, "stroke"),
                                Opacity = ReadFloat(element, "stroke-opacity", 1)
                            };
                        }
                    }

                    shape.StrokeWidth = ReadFloat(element, "stroke-width");

                    var strokeStyle = new StrokeStyleProperties1();

                    if (element.Attribute("stroke-dasharray") != null)
                    {
                        strokeStyle.DashStyle = DashStyle.Custom;
                        shape.StrokeDashes =
                            new ObservableCollection<float>(
                                ReadFloats(element, "stroke-dasharray")
                                    .Select(f => f / Math.Max(shape.StrokeWidth, 1e-10f)));
                    }

                    switch ((string)element.Attribute("stroke-linejoin"))
                    {
                        case "miter":
                            strokeStyle.LineJoin = LineJoin.Miter;
                            break;
                        case "bevel":
                            strokeStyle.LineJoin = LineJoin.Bevel;
                            break;
                        case "round":
                            strokeStyle.LineJoin = LineJoin.Round;
                            break;
                        default:
                            break;
                    }

                    switch ((string)element.Attribute("stroke-linecap"))
                    {
                        case "butt":
                            strokeStyle.StartCap = strokeStyle.EndCap = strokeStyle.DashCap = CapStyle.Flat;
                            break;
                        case "square":
                            strokeStyle.StartCap = strokeStyle.EndCap = strokeStyle.DashCap = CapStyle.Square;
                            break;
                        case "round":
                            strokeStyle.StartCap = strokeStyle.EndCap = strokeStyle.DashCap = CapStyle.Round;
                            break;
                        case "triangle":
                            strokeStyle.StartCap = strokeStyle.EndCap = strokeStyle.DashCap = CapStyle.Triangle;
                            break;
                        default:
                            break;
                    }

                    shape.StrokeStyle = strokeStyle;
                }

                layer.Name = element.Attribute("id")?.Value;

                var transform = element.Attribute("transform")?.Value;

                if (transform != null)
                {
                    if (transform.StartsWith("rotate"))
                        layer.Rotation = ReadFloat(element, "transform");

                    if (transform.StartsWith("translate"))
                        layer.Position += ReadVector(element, "transform");

                    if (transform.StartsWith("scale"))
                        layer.Scale *= ReadVector(element, "transform");

                    if (transform.StartsWith("skewY"))
                        layer.Shear += ReadFloat(element, "transform");

                    if (transform.StartsWith("skewX"))
                    {
                        layer.Shear += ReadFloat(element, "transform");
                        layer.Rotation += -layer.Shear;
                    }

                    if (transform.StartsWith("matrix"))
                    {
                        var m = ReadMatrix(element, "transform");
                        var d = m.Decompose();
                        layer.Scale *= d.scale;
                        layer.Rotation += d.rotation;
                        layer.Shear += d.skew;
                        layer.Position += d.translation;
                    }
                }

                return layer as T;
            }

            #endregion


            return null;
        }

        private static object Resolve(XContainer doc, string iri)
        {
            var parts = iri.Split(new [] {'#'}, StringSplitOptions.RemoveEmptyEntries);
            var name = parts.LastOrDefault();

            if (name == null) return null;

            if (parts.Length == 2)
                return 
                    Resolve(
                        XDocument.Load(
                            File.Open(parts[0], FileMode.Open, FileAccess.Read)), "#" + parts[1]);

            var element = 
                doc.Descendants()
                    .FirstOrDefault(x => (string) x.Attribute("id") == name);

            return element == null ? null : Parse<object>(element, null, doc);
        }

        private static Color4 ReadColor(XElement element, string attr)
        {
            var value = element.Attribute(attr)?.Value ?? "";

            if (value.StartsWith("#"))
            {
                var r = Convert.ToByte(value.Substring(1, 2), 16) / 255f;
                var g = Convert.ToByte(value.Substring(3, 2), 16) / 255f;
                var b = Convert.ToByte(value.Substring(5, 2), 16) / 255f;

                return new Color4(r, g, b, 1);
            }

            var values = Extract(value, UnitType.None, 255).Select(v => v / 255).ToArray();

            if (values.Length == 3)
                return new Color4(new Color3(values), 1);
            if (values.Length == 4)
                return new Color4(values);

            throw new FormatException();
        }

        private static float ReadFloat(XElement element, string attr, float defaultValue = 0)
        {
            var input = element.Attribute(attr)?.Value;

            if (input == null) return defaultValue;

            var value = Extract(input, UnitType.None, 1);

            return value.Length > 0 ? value[0] : defaultValue;
        }

        private static float[] ReadFloats(XElement element, string attr)
        {
            var input = element.Attribute(attr)?.Value;

            return input == null ? new float[0] : Extract(input, UnitType.None, 1);
        }

        private static Matrix3x2 ReadMatrix(XElement element, string attr)
        {
            var values = Extract(element.Attribute(attr)?.Value);

            if (values.Length == 6)
                return new Matrix3x2(values);

            throw new FormatException();
        }

        private static Vector2 ReadVector(XElement element, string attr)
        {
            var values = Extract(element.Attribute(attr)?.Value);

            if (values.Length == 2)
                return new Vector2(values);

            throw new FormatException();
        }
    }

    internal static class PathDataSerializer
    {
        public static IEnumerable<PathNode> Parse(string data)
        {
            var nodes = new List<PathNode>();

            var commands = Regex.Matches(data ?? "",
                @"([MLHVCTSAZmlhvctsaz]){1}\s*(?:,?(\s*(?:[-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:[Ee][-+]?[0-9]+)?)\s*))*");
            var (start, pos, control, control2) = (Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);
            var lastInstruction = PathDataInstruction.Close;

            foreach (Match command in commands)
            {
                var parameters =
                    from set in command.Groups
                        .OfType<System.Text.RegularExpressions.Group>()
                        .Skip(2)
                    from Capture cap in set.Captures
                    select float.Parse(cap.Value);

                var coordinates = new Stack<float>(parameters.Reverse());
                var relative = char.IsLower(command.Groups[1].Value[0]);
                PathDataInstruction instruction;

                switch (char.ToUpper(command.Groups[1].Value[0]))
                {
                    case 'M':
                        instruction = PathDataInstruction.Move;
                        break;
                    case 'L':
                        instruction = PathDataInstruction.Line;
                        break;
                    case 'H':
                        instruction = PathDataInstruction.Horizontal;
                        break;
                    case 'V':
                        instruction = PathDataInstruction.Vertical;
                        break;
                    case 'C':
                        instruction = PathDataInstruction.Cubic;
                        break;
                    case 'S':
                        instruction = PathDataInstruction.ShortCubic;
                        break;
                    case 'Q':
                        instruction = PathDataInstruction.Quadratic;
                        break;
                    case 'T':
                        instruction = PathDataInstruction.ShortQuadratic;
                        break;
                    case 'A':
                        instruction = PathDataInstruction.Arc;
                        break;
                    case 'Z':
                        instruction = PathDataInstruction.Close;
                        break;
                    default:
                        throw new InvalidDataException("Invalid command.");
                }

                if (instruction != PathDataInstruction.Move &&
                    (lastInstruction == PathDataInstruction.Move ||
                     lastInstruction == PathDataInstruction.Close))
                    nodes.Add(new PathNode {X = pos.X, Y = pos.Y});

                switch (instruction)
                {
                    case PathDataInstruction.Move:
                        if (relative)
                            pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                        else
                            pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                        if (lastInstruction == PathDataInstruction.Close)
                        {
                            start = pos;
                            instruction = PathDataInstruction.Close;
                        }

                        if (coordinates.Count >= 2)
                        {
                            instruction = PathDataInstruction.Line;

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});

                            while (coordinates.Count >= 2)
                            {
                                if (relative)
                                    pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                                else
                                    pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                                nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                            }
                        }
                        break;

                    #region Linear

                    case PathDataInstruction.Line:
                        while (coordinates.Count >= 2)
                        {
                            if (relative)
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            else
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }

                        break;

                    case PathDataInstruction.Horizontal:
                        while (coordinates.Count >= 1)
                        {
                            if (relative)
                                pos.X += coordinates.Pop();
                            else
                                pos.X = coordinates.Pop();

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }
                        break;

                    case PathDataInstruction.Vertical:
                        while (coordinates.Count >= 1)
                        {
                            if (relative)
                                pos.Y += coordinates.Pop();
                            else
                                pos.Y = coordinates.Pop();

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }
                        break;

                    #endregion

                    #region Quadratic

                    case PathDataInstruction.Quadratic:
                        while (coordinates.Count >= 4)
                        {
                            if (relative)
                            {
                                control = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new QuadraticPathNode
                            {
                                Control = control,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;
                    case PathDataInstruction.ShortQuadratic:
                        while (coordinates.Count >= 2)
                        {
                            control = pos - (control - pos);

                            if (relative)
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            else
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                            nodes.Add(new QuadraticPathNode
                            {
                                Control = control,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;

                    #endregion

                    #region Cubic

                    case PathDataInstruction.Cubic:
                        while (coordinates.Count >= 6)
                        {
                            if (relative)
                            {
                                control = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                control2 = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control = new Vector2(coordinates.Pop(), coordinates.Pop());
                                control2 = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new CubicPathNode
                            {
                                Control1 = control,
                                Control2 = control2,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;
                    case PathDataInstruction.ShortCubic:
                        while (coordinates.Count >= 4)
                        {
                            control = pos - (control - pos);

                            if (relative)
                            {
                                control2 = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control2 = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new CubicPathNode
                            {
                                Control1 = control,
                                Control2 = control2,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;

                    #endregion

                    case PathDataInstruction.Arc:
                        throw new NotImplementedException("Arc instructions in paths are not supported yet.");

                    case PathDataInstruction.Close:
                        nodes.Add(new CloseNode());
                        pos = start;
                        break;
                }

                lastInstruction = instruction;
            }

            return nodes;
        }

        #region Nested type: PathDataInstruction

        private enum PathDataInstruction
        {
            Move,
            Line,
            Horizontal,
            Vertical,
            Cubic,
            ShortCubic,
            Quadratic,
            ShortQuadratic,
            Arc,
            Close
        }

        #endregion
    }
}