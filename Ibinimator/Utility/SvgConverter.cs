using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using static Ibinimator.Svg.LengthUnit;

namespace Ibinimator.Utility
{
    public static class SvgConverter
    {
        public static Model.Document FromSvg(Svg.Document svgDocument)
        {
            var doc = new Model.Document
            {
                Root = new Model.Group()
            };

            foreach (var child in svgDocument.Select(FromSvg))
                doc.Root.Add(child);

            return doc;
        }

        public static Model.Layer FromSvg(Svg.IElement element)
        {
            Model.Layer layer = null;

            if (element is Svg.IContainerElement containerElement)
            {
                var group = new Model.Group();

                foreach (var child in containerElement.Select(FromSvg))
                    group.Add(child);

                layer = group;
            }

            if (element is Svg.IShapeElement shapeElement)
            {
                Model.Shape shape = null;

                switch (shapeElement)
                {
                    case Svg.Ellipse ellipse:
                        shape = new Model.Ellipse
                        {
                            CenterX = ellipse.CenterX,
                            CenterY = ellipse.CenterY,
                            RadiusX = ellipse.RadiusX.To(Pixels),
                            RadiusY = ellipse.RadiusY.To(Pixels)
                        };
                        break;
                    case Svg.Line line:
                        var linePath = new Model.Path();
                        linePath.Nodes.Add(new Model.PathNode {X = line.X1, Y = line.Y1});
                        linePath.Nodes.Add(new Model.PathNode {X = line.X2, Y = line.Y2});
                        shape = linePath;
                        break;
                    case Svg.Path path:
                        var pathPath = new Model.Path();

                        pathPath.Nodes.AddItems(path.Data.Select(pathNode =>
                        {
                            switch (pathNode)
                            {
                                case Svg.ArcPathNode arcPathNode:
                                    return new Model.ArcPathNode
                                    {
                                        Clockwise = arcPathNode.Clockwise,
                                        LargeArc = arcPathNode.LargeArc,
                                        Position = arcPathNode.Position.Convert(),
                                        RadiusX = arcPathNode.RadiusX,
                                        RadiusY = arcPathNode.RadiusY,
                                        Rotation = arcPathNode.Rotation
                                    };
                                case Svg.CloseNode closeNode:
                                    return new Model.CloseNode
                                    {
                                        Open = closeNode.Open
                                    };
                                case Svg.CubicPathNode cubicPathNode:
                                    return new Model.CubicPathNode
                                    {
                                        Control1 = cubicPathNode.Control1.Convert(),
                                        Control2 = cubicPathNode.Control2.Convert(),
                                        Position = cubicPathNode.Position.Convert()
                                    };
                                case Svg.QuadraticPathNode quadraticPathNode:
                                    return new Model.QuadraticPathNode
                                    {
                                        Control = quadraticPathNode.Control.Convert(),
                                        Position = quadraticPathNode.Position.Convert()
                                    };
                                default:
                                    return new Model.PathNode
                                    {
                                        Position = pathNode.Position.Convert()
                                    };
                            }
                        }));

                        shape = pathPath;
                        break;
                    case Svg.Polygon polygon:
                        var polygonPath = new Model.Path();

                        polygonPath.Nodes.AddItems(
                            polygon.Points.Select(v => new Model.PathNode
                            {
                                Position = v.Convert()
                            }));

                        polygonPath.Nodes.Add(new Model.CloseNode {Open = false});

                        shape = polygonPath;
                        break;
                    case Svg.Polyline polyline:
                        var polylinePath = new Model.Path();

                        polylinePath.Nodes.AddItems(
                            polyline.Points.Select(v => new Model.PathNode
                            {
                                Position = v.Convert()
                            }));

                        polylinePath.Nodes.Add(new Model.CloseNode {Open = true});

                        shape = polylinePath;
                        break;
                    case Svg.Rectangle rectangle:
                        shape = new Model.Rectangle
                        {
                            X = rectangle.X,
                            Y = rectangle.Y,
                            Width = rectangle.Width.To(Pixels),
                            Height = rectangle.Height.To(Pixels)
                        };
                        break;
                    case Svg.Circle circle:
                        shape = new Model.Ellipse
                        {
                            CenterX = circle.CenterX,
                            CenterY = circle.CenterY,
                            RadiusX = circle.Radius.To(Pixels),
                            RadiusY = circle.Radius.To(Pixels)
                        };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(shapeElement));
                }

                shape.FillBrush = FromSvg(shapeElement.Fill, shapeElement.FillOpacity);
                shape.StrokeBrush = FromSvg(shapeElement.Fill, shapeElement.FillOpacity);

                layer = shape;
            }

            return layer;
        }

        public static Model.BrushInfo FromSvg(Svg.Paint? paint, float opacity)
        {
            if (paint?.Color != null)
            {
                var color = paint.Value.Color.Value;

                return new Model.SolidColorBrushInfo(
                    new Color4(
                        color.Red,
                        color.Green,
                        color.Blue,
                        color.Alpha * opacity));
            }

            return null;
        }
    }
}