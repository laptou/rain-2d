using System;
using System.Collections.Generic;
using System.Diagnostics;

using Ibinimator.Svg.Paint;

using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Model.Text;
using Ibinimator.Svg.Shapes;
using Ibinimator.Svg.Structure;

using Core = Ibinimator.Core.Model;
using DG = Ibinimator.Core.Model.DocumentGraph;
using SVG = Ibinimator.Svg;
using Structure = Ibinimator.Svg.Structure;

namespace Ibinimator.Svg.IO
{
    public static class SvgWriter
    {
        public static Document ToSvg(DG.Document doc)
        {
            var svgDoc = new Document();

            var nodes = Crawl(doc).ToArray();
            var defs = new Defs();
            svgDoc.Add(defs);

            IElement previous = svgDoc;
            Node previousNode = nodes.First();

            IDictionary<Node, IElement> table = new Dictionary<Node, IElement>
            {
                {previousNode, previous}
            };

            foreach (var node in nodes)
            {
                // nodes will always be listed after their parent,
                // which means you can trust that the last parent posted
                // is the parent of the current node
                if (node.Parent != null)
                {
                    IElement parent = node.Rank > previousNode.Rank ? previous : table[node.Parent];

                    var element = ToSvg(node, parent);

                    if(RequiresDef(node))
                        svgDoc.Defs.Add(element);

                    if (element != null)
                    {
                        table[node] = element;
                        previous = element;
                        previousNode = node;
                    }
                }
            }

            svgDoc.Viewbox = doc.Bounds;

            return svgDoc;
        }

        private static IEnumerable<Node> Crawl(DG.Document doc)
        {
            var root = new Node(null, doc, null);

            yield return root;

            foreach (var swatch in doc.Swatches)
                yield return new Node(swatch.Name, swatch, root);

            foreach (var layer in doc.Root.SubLayers.Reverse())
                foreach (var node in Crawl(layer, root))
                    yield return node;
        }

        private static IEnumerable<Node> Crawl(IBrushInfo brush, Node ancestor)
        {
            var node = new Node(brush.Name, brush, ancestor);

            yield return node;

            if (brush is GradientBrushInfo gradient)
                foreach (var stop in gradient.Stops)
                    yield return new Node(null, stop, node);
        }

        private static IEnumerable<Node> Crawl(IPenInfo pen, Node ancestor)
        {
            var node = new Node(null, pen, ancestor);

            yield return node;

            if (pen.Brush != null)
                foreach (var n in Crawl(pen.Brush, node))
                    yield return n;
        }

        private static IEnumerable<Node> Crawl(DG.ILayer layer, Node ancestor)
        {
            var root = new Node(layer.Name, layer, ancestor);

            yield return root;

            if (layer is DG.IFilledLayer filled &&
                filled.Fill != null)
            {
                foreach (var node in Crawl(filled.Fill, root))
                    yield return node;
            }

            if (layer is DG.IStrokedLayer stroked &&
                stroked.Stroke != null)
            {
                foreach (var node in Crawl(stroked.Stroke, root))
                        yield return node;
            }

            if (layer is DG.ITextLayer text &&
                text.Value != null)
            {
                foreach (var node in Crawl(text, root))
                    if (RequiresDef(node))
                        yield return node;
            }

            if (layer is DG.IContainerLayer container)
                foreach (var sublayer in container.SubLayers.Reverse())
                    foreach (var node in Crawl(sublayer, root))
                        yield return node;
        }

        private static IEnumerable<Node> Crawl(DG.ITextLayer layer, Node ancestor)
        {
            var node = new Node(layer.Name, layer, ancestor);

            foreach (var format in layer.Formats)
                yield return new Node(null, format, node);
        }

        private static bool RequiresDef(Node node)
        {
            if (node.Target is GradientBrushInfo)
                return true;

            if (node.Target is IBrushInfo brushInfo &&
                brushInfo.Scope == ResourceScope.Document)
                return true;

            return false;
        }

        private static IElement ToSvg(Node node, IElement parent)
        {
            IElement element = null;

            if (node.Target is IBrushInfo brushInfo)
            {
                element = brushInfo.Convert();

                if (parent is IShapeElement shape)
                {
                    shape.Fill = (Paint.Paint) element;
                    shape.FillOpacity = brushInfo.Opacity;
                }
            }

            if (node.Target is IPenInfo pen)
            {
                if (pen.Brush != null)
                    element = pen.Brush.Convert();

                if (parent is IShapeElement shape)
                {
                    shape.Stroke = (Paint.Paint)element;
                    shape.StrokeOpacity = pen.Brush?.Opacity ?? 1;

                    shape.StrokeDashArray = pen.Dashes.Select(f => f * pen.Width).ToArray();
                    shape.StrokeWidth = (pen.Width, LengthUnit.Pixels);
                    shape.StrokeDashOffset = pen.DashOffset;
                    shape.StrokeLineCap = pen.LineCap;
                    shape.StrokeLineJoin = pen.LineJoin;
                    shape.StrokeMiterLimit = pen.MiterLimit / pen.Width;
                }
            }

            if (node.Target is Format textFormat)
            {
                var text = (DG.ITextLayer) node.Parent.Target;

                element = new Span
                {
                    FontFamily = textFormat.FontFamilyName,
                    FontStretch = textFormat.FontStretch,
                    FontWeight = textFormat.FontWeight,
                    FontStyle = textFormat.FontStyle,
                    FontSize =
                        textFormat.FontSize != null
                            ? new Length?(new Length(textFormat.FontSize.Value, LengthUnit.Points))
                            : null,
                    Text = text.Value?.Substring(textFormat.Range.Index, textFormat.Range.Length),
                    Position = textFormat.Range.Index
                };
            }

            if (node.Target is DG.ITextLayer textLayer)
            {
                element = new Text
                {
                    FontFamily = textLayer.FontFamilyName,
                    FontStretch = textLayer.FontStretch,
                    FontWeight = textLayer.FontWeight,
                    FontStyle = textLayer.FontStyle,
                    FontSize = new Length(textLayer.FontSize, LengthUnit.Points),
                    Text = textLayer.Value,
                    Y = textLayer.Baseline
                };
            }

            if (node.Target is DG.IGeometricLayer geomLayer)
            {
                switch (geomLayer)
                {
                    case DG.Ellipse ellipse:
                        element = new Shapes.Ellipse
                        {
                            CenterX = new Length(ellipse.CenterX, LengthUnit.Pixels),
                            CenterY = new Length(ellipse.CenterY, LengthUnit.Pixels),
                            RadiusX = new Length(ellipse.RadiusX, LengthUnit.Pixels),
                            RadiusY = new Length(ellipse.RadiusY, LengthUnit.Pixels)
                        };

                        break;
                    case DG.Path path:
                        element = new Shapes.Path
                        {
                            Data = path.Instructions.ToArray()
                        };

                        break;
                    case DG.Rectangle rectangle:
                        element = new Shapes.Rectangle
                        {
                            X = new Length(rectangle.X, LengthUnit.Pixels),
                            Y = new Length(rectangle.Y, LengthUnit.Pixels),
                            Width = new Length(rectangle.Width, LengthUnit.Pixels),
                            Height = new Length(rectangle.Height, LengthUnit.Pixels)
                        };

                        break;
                }
            }

            if (node.Target is DG.IContainerLayer containerLayer)
            {
                element = new Group();
            }

            if (node.Target is DG.ILayer layer && element is IGraphicalElement graphicalElement)
            {
                graphicalElement.Transform = layer.Transform;
            }

            if (element != null)
            {
                element.Id = node.Id;
                element.Name = node.Name;

                if (parent is IContainerElement containerElement)
                    containerElement.Add(element);
            }

            return element;
        }


        #region Nested type: Node

        [DebuggerDisplay("#{Rank}::{Id}::{Target.GetType()} < {Parent?.Target.GetType()}")]
        private class Node
        {
            public Node(string name, object target, Node parent)
            {
                Target = target;

                if (parent != null)
                {
                    Parent = parent;
                    Rank = parent.Rank + 1;
                }

                Name = name;
            }

            public string Name { get; }

            public string Id
            {
                get
                {
                    var prefix = (char)(97 + Rank % 26);
                    var suffix = unchecked((uint)Target.GetHashCode()).ToString();
                    return string.Join("_", prefix, Name, suffix);
                }
            }

            public Node Parent { get; }
            public int Rank { get; }
            public object Target { get; }
        }

        #endregion
    }
}