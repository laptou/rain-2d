using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
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
                doc.Root.Add(child, 0);

            return doc;
        }

        public static Model.Layer FromSvg(Svg.IElement element)
        {
            Model.Layer layer = null;

            if (element is Svg.IContainerElement containerElement)
            {
                var group = new Model.Group();

                foreach (var child in containerElement.Select(FromSvg))
                    group.Add(child, 0);

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
                shape.StrokeBrush = FromSvg(shapeElement.Stroke, shapeElement.StrokeOpacity);

                var dashes = shapeElement.StrokeDashArray.Select(f => f / shapeElement.StrokeWidth.To(Pixels)).ToArray();

                shape.StrokeInfo = new Model.StrokeInfo
                {
                    Width = shapeElement.StrokeWidth.To(Pixels),
                    Dashes = new ObservableList<float>(dashes),
                    Style = new StrokeStyleProperties1
                    {
                        DashStyle = dashes.Any() ? DashStyle.Custom : DashStyle.Solid
                    }
                };

                layer = shape;
            }

            if (element is Svg.IGraphicalElement graphicalElement)
            {
                (layer.Scale, layer.Rotation, layer.Position, layer.Shear) = 
                    graphicalElement.Transform.Convert().Decompose();
            }

            layer.Name = element.Id;

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

        public static Svg.Document ToSvg(Model.Document doc)
        {
            var svgDoc = new Svg.Document();

            foreach (var child in doc.Root.SubLayers.Select(ToSvg))
                svgDoc.Add(child);

            return svgDoc;
        }

        public static Svg.IElement ToSvg(Model.Layer layer)
        {
            Svg.IGraphicalElement element = null;

            if (layer is Model.Group groupLayer)
            {
                var group = new Svg.Group();

                foreach (var child in groupLayer.SubLayers.Select(ToSvg))
                    group.Add(child);

                element = group;
            }

            if (layer is Model.Shape shapeLayer)
            {
                Svg.IShapeElement shape = null;

                switch (shapeLayer)
                {
                    case Model.Ellipse ellipse:
                        shape = new Svg.Ellipse
                        {
                            CenterX = ellipse.RadiusX,
                            CenterY = ellipse.RadiusY,
                            RadiusX = new Svg.Length(ellipse.RadiusX, Pixels),
                            RadiusY = new Svg.Length(ellipse.RadiusY, Pixels)
                        };
                        break;
                    case Model.Path path:
                        var pathPath = new Svg.Path();

                        pathPath.Data = path.Nodes.Select(pathNode =>
                        {
                            switch (pathNode)
                            {
                                case Model.ArcPathNode arcPathNode:
                                    return new Svg.ArcPathNode
                                    {
                                        Clockwise = arcPathNode.Clockwise,
                                        LargeArc = arcPathNode.LargeArc,
                                        Position = arcPathNode.Position.Convert(),
                                        RadiusX = arcPathNode.RadiusX,
                                        RadiusY = arcPathNode.RadiusY,
                                        Rotation = arcPathNode.Rotation
                                    };
                                case Model.CloseNode closeNode:
                                    return new Svg.CloseNode
                                    {
                                        Open = closeNode.Open
                                    };
                                case Model.CubicPathNode cubicPathNode:
                                    return new Svg.CubicPathNode
                                    {
                                        Control1 = cubicPathNode.Control1.Convert(),
                                        Control2 = cubicPathNode.Control2.Convert(),
                                        Position = cubicPathNode.Position.Convert()
                                    };
                                case Model.QuadraticPathNode quadraticPathNode:
                                    return new Svg.QuadraticPathNode
                                    {
                                        Control = quadraticPathNode.Control.Convert(),
                                        Position = quadraticPathNode.Position.Convert()
                                    };
                                default:
                                    return new Svg.PathNode
                                    {
                                        Position = pathNode.Position.Convert()
                                    };
                            }
                        }).ToArray();

                        shape = pathPath;
                        break;
                    case Model.Rectangle rectangle:
                        shape = new Svg.Rectangle
                        {
                            Width = new Svg.Length(rectangle.Width, Pixels),
                            Height = new Svg.Length(rectangle.Height, Pixels)
                        };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(shapeLayer));
                }

                shape.Fill = ToSvg(shapeLayer.FillBrush);
                shape.FillOpacity = shapeLayer.FillBrush?.Opacity ?? 0;

                shape.Stroke = ToSvg(shapeLayer.StrokeBrush);
                shape.StrokeOpacity = shapeLayer.StrokeBrush?.Opacity ?? 0;

                var dashes = shapeLayer.StrokeInfo.Dashes.Select(f => f * shapeLayer.StrokeInfo.Width).ToArray();

                shape.StrokeWidth = new Svg.Length(shapeLayer.StrokeInfo.Width, Pixels);
                shape.StrokeDashArray = dashes.ToArray();

                element = shape;
            }

            if (element != null)
            {
                element.Transform = layer.Transform.Convert();

                element.Id = layer.Name;
            }

            return element;
        }

        public static Svg.Paint? ToSvg(Model.BrushInfo brush)
        {
            if (brush is Model.SolidColorBrushInfo solidBrush)
            {
                return new Svg.Paint(
                    new Svg.Color(
                        solidBrush.Color.Red,
                        solidBrush.Color.Green,
                        solidBrush.Color.Blue,
                        solidBrush.Color.Alpha
                        ));
            }

            return null;
        }
    }
}