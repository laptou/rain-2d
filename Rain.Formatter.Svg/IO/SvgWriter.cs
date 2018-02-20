using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Formatter.Svg.Paint;
using Rain.Formatter.Svg.Shapes;
using Rain.Formatter.Svg.Structure;

using Document = Rain.Formatter.Svg.Structure.Document;
using Ellipse = Rain.Formatter.Svg.Shapes.Ellipse;
using Group = Rain.Formatter.Svg.Structure.Group;
using Path = Rain.Formatter.Svg.Shapes.Path;
using Rectangle = Rain.Formatter.Svg.Shapes.Rectangle;
using Text = Rain.Formatter.Svg.Shapes.Text;

namespace Rain.Formatter.Svg.IO
{
    public static class SvgWriter
    {
        public static Document ToSvg(Core.Model.DocumentGraph.Document doc)
        {
            var svgDoc = new Document();

            var nodes = Utility.Crawl(doc).ToArray();
            var lookup = nodes.ToLookup(n => n.Target);
            var defs = new Defs();
            svgDoc.Add(defs);

            IElement previous = svgDoc;
            var previousNode = nodes.First();

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
                    var parent = node.Rank > previousNode.Rank ? previous : table[node.Parent];

                    var element = ToSvg(node, parent, lookup);

                    if (RequiresDef(node))
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
        
        private static bool RequiresDef(Node node)
        {
            if (node.Target is GradientBrushInfo)
                return true;

            if (node.Target is IBrushInfo brushInfo &&
                brushInfo.Scope == ResourceScope.Document)
                return true;

            return false;
        }

        private static IElement ToSvg(Node node, IElement parent, ILookup<object, Node> lookup)
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
                    shape.Stroke = (Paint.Paint) element;
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
                var text = (ITextLayer) node.Parent.Target;

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

            if (node.Target is ITextLayer textLayer)
                element = new Text
                {
                    FontFamily = textLayer.TextStyle.FontFamily,
                    FontStretch = textLayer.TextStyle.FontStretch,
                    FontWeight = textLayer.TextStyle.FontWeight,
                    FontStyle = textLayer.TextStyle.FontStyle,
                    FontSize = new Length(textLayer.TextStyle.FontSize, LengthUnit.Points),
                    Text = textLayer.Value,
                    Y = textLayer.TextStyle.Baseline
                };

            if (node.Target is IGeometricLayer geomLayer)
                switch (geomLayer)
                {
                    case Core.Model.DocumentGraph.Ellipse ellipse:
                        element = new Ellipse
                        {
                            CenterX = new Length(ellipse.CenterX, LengthUnit.Pixels),
                            CenterY = new Length(ellipse.CenterY, LengthUnit.Pixels),
                            RadiusX = new Length(ellipse.RadiusX, LengthUnit.Pixels),
                            RadiusY = new Length(ellipse.RadiusY, LengthUnit.Pixels)
                        };

                        break;
                    case Core.Model.DocumentGraph.Path path:
                        element = new Path
                        {
                            Data = path.Instructions.ToArray()
                        };

                        break;
                    case Core.Model.DocumentGraph.Rectangle rectangle:
                        element = new Rectangle
                        {
                            X = new Length(rectangle.X, LengthUnit.Pixels),
                            Y = new Length(rectangle.Y, LengthUnit.Pixels),
                            Width = new Length(rectangle.Width, LengthUnit.Pixels),
                            Height = new Length(rectangle.Height, LengthUnit.Pixels)
                        };

                        break;
                }

            if (node.Target is IContainerLayer)
                element = new Group();

            if (node.Target is ICloneLayer cloneLayer)
            {
                var target = lookup[cloneLayer.Target].First();

                element = new Use
                {
                    Target = Iri.FromId(target.Id)
                };
            }

            if (node.Target is ILayer layer &&
                element is IGraphicalElement graphicalElement)
                graphicalElement.Transform = layer.Transform;

            if (element != null)
            {
                element.Id = node.Id;
                element.Name = node.Name;

                if (parent is IContainerElement containerElement)
                    containerElement.Add(element);
            }

            return element;
        }
    }
}