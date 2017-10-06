using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using static Ibinimator.Svg.LengthUnit;
using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontWeight = SharpDX.DirectWrite.FontWeight;
using LineJoin = SharpDX.Direct2D1.LineJoin;

namespace Ibinimator.Utility
{
    public static class SvgConverter
    {
        public static Model.Document FromSvg(Svg.Document svgDocument)
        {
            var doc = new Model.Document
            {
                Root = new Model.Group(),
                Bounds = new RectangleF(
                    svgDocument.Viewbox.X, svgDocument.Viewbox.Y,
                    svgDocument.Viewbox.Width, svgDocument.Viewbox.Height)
            };

            foreach (var child in svgDocument.OfType<Svg.IGraphicalElement>().Select(FromSvg))
                doc.Root.Add(child, 0);

            return doc;
        }

        public static Model.Layer FromSvg(Svg.IGraphicalElement element)
        {
            Model.Layer layer = null;

            if (element is Svg.IContainerElement containerElement)
            {
                var group = new Model.Group();

                foreach (var child in containerElement.OfType<Svg.IGraphicalElement>().Select(FromSvg))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is Svg.IShapeElement shapeElement)
            {
                Model.IGeometricLayer shape = null;

                switch (shapeElement)
                {
                    case Svg.Ellipse ellipse:
                        shape = new Model.Ellipse
                        {
                            CenterX = ellipse.CenterX.To(Pixels),
                            CenterY = ellipse.CenterY.To(Pixels),
                            RadiusX = ellipse.RadiusX.To(Pixels),
                            RadiusY = ellipse.RadiusY.To(Pixels)
                        };
                        break;
                    case Svg.Line line:
                        var linePath = new Model.Path();

                        linePath.Nodes.Add(new Model.PathNode
                        {
                            X = line.X1.To(Pixels),
                            Y = line.Y1.To(Pixels)
                        });

                        linePath.Nodes.Add(new Model.PathNode
                        {
                            X = line.X2.To(Pixels),
                            Y = line.Y2.To(Pixels)
                        });

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
                            X = rectangle.X.To(Pixels),
                            Y = rectangle.Y.To(Pixels),
                            Width = rectangle.Width.To(Pixels),
                            Height = rectangle.Height.To(Pixels)
                        };
                        break;
                    case Svg.Circle circle:
                        shape = new Model.Ellipse
                        {
                            CenterX = circle.CenterX.To(Pixels),
                            CenterY = circle.CenterY.To(Pixels),
                            RadiusX = circle.Radius.To(Pixels),
                            RadiusY = circle.Radius.To(Pixels)
                        };
                        break;
                    case Svg.Text text:
                        shape = new Model.Text
                        {
                            FontFamilyName = text.FontFamily ?? "Arial",
                            FontStretch = (FontStretch) (text.FontStretch ?? Svg.FontStretch.Normal),
                            FontWeight = (FontWeight) (text.FontWeight ?? Svg.FontWeight.Normal),
                            FontSize = text.FontSize?.To(Points) ?? 12,
                            Value = text.Text
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
                        DashStyle = dashes.Any() ? DashStyle.Custom : DashStyle.Solid,
                        DashCap = (CapStyle)shapeElement.StrokeLineCap,
                        StartCap = (CapStyle)shapeElement.StrokeLineCap,
                        EndCap = (CapStyle)shapeElement.StrokeLineCap,
                        LineJoin = (LineJoin)shapeElement.StrokeLineJoin,
                        DashOffset = shapeElement.StrokeDashOffset,
                        MiterLimit = shapeElement.StrokeMiterLimit
                    }
                };

                layer = shape as Model.Layer;
            }

            if (layer != null)
            {

                (layer.Scale, layer.Rotation, layer.Position, layer.Shear) =
                    element.Transform.Convert().Decompose();

                layer.Name = element.Id;
            }

            return layer;
        }

        public static Model.BrushInfo FromSvg(Svg.Paint paint, float opacity)
        {
            switch (paint)
            {
                case Svg.SolidColor solidColor:
                    var color = solidColor.Color;

                    return new Model.SolidColorBrushInfo(
                        new Color4(
                            color.Red,
                            color.Green,
                            color.Blue,
                            color.Alpha * opacity));

                case Svg.LinearGradient linearGradient:
                    return new Model.GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(linearGradient.Stops.Select(s => new GradientStop
                        {
                            Color = new Color4(
                                s.Color.Red,
                                s.Color.Green,
                                s.Color.Blue,
                                s.Color.Alpha),
                            Position = s.Offset.To(Pixels, 1)
                        })),
                        StartPoint = new Vector2(linearGradient.X1.To(Pixels, 1), linearGradient.Y1.To(Pixels, 1)),
                        EndPoint = new Vector2(linearGradient.X2.To(Pixels, 1), linearGradient.Y2.To(Pixels, 1)),
                        Name = linearGradient.Id,
                        Transform = linearGradient.Transform.Convert(),
                        ExtendMode = (ExtendMode)linearGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = Model.GradientBrushType.Linear
                    };

                case Svg.RadialGradient radialGradient:
                    return new Model.GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(radialGradient.Stops.Select(s => new GradientStop
                        {
                            Color = new Color4(
                                s.Color.Red,
                                s.Color.Green,
                                s.Color.Blue,
                                s.Color.Alpha),
                            Position = s.Offset.To(Pixels, 1)
                        })),
                        StartPoint = new Vector2(
                            radialGradient.CenterX.To(Pixels, 1), 
                            radialGradient.CenterY.To(Pixels, 1)),
                        Focus = new Vector2(
                            radialGradient.FocusX.To(Pixels, 1), 
                            radialGradient.FocusY.To(Pixels, 1)),
                        EndPoint = new Vector2(
                            radialGradient.CenterX.To(Pixels, 1) + radialGradient.Radius.To(Pixels, 1),
                            radialGradient.CenterX.To(Pixels, 1) + radialGradient.Radius.To(Pixels, 1)),
                        Name = radialGradient.Id,
                        Transform = radialGradient.Transform.Convert(),
                        ExtendMode = (ExtendMode)radialGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = Model.GradientBrushType.Radial
                    };
            }

            return null;
        }

        public static Svg.Document ToSvg(Model.Document doc)
        {
            var svgDoc = new Svg.Document();

            foreach (var child in doc.Root.SubLayers.Select(ToSvg))
                svgDoc.Insert(0, child);

            var bounds = doc.Bounds;

            svgDoc.Viewbox = new System.Drawing.RectangleF(
                bounds.X, bounds.Y, bounds.Width, bounds.Height);

            return svgDoc;
        }

        public static Svg.IElement ToSvg(Model.Layer layer)
        {
            Svg.IGraphicalElement element = null;

            if (layer is Model.Group groupLayer)
            {
                var group = new Svg.Group();

                foreach (var child in groupLayer.SubLayers.Select(ToSvg))
                    group.Insert(0, child);

                element = group;
            }

            if (layer is Model.IGeometricLayer shapeLayer)
            {
                Svg.IShapeElement shape = null;

                switch (shapeLayer)
                {
                    case Model.Ellipse ellipse:
                        shape = new Svg.Ellipse
                        {
                            CenterX = new Svg.Length(ellipse.CenterX, Pixels),
                            CenterY = new Svg.Length(ellipse.CenterY, Pixels),
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
                            X = new Svg.Length(rectangle.X, Pixels),
                            Y = new Svg.Length(rectangle.Y, Pixels),
                            Width = new Svg.Length(rectangle.Width, Pixels),
                            Height = new Svg.Length(rectangle.Height, Pixels)
                        };
                        break;
                    case Model.Text text:
                        shape = ToSvg(text);
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
                shape.StrokeDashOffset = shapeLayer.StrokeInfo.Style.DashOffset;
                shape.StrokeLineCap = (Svg.LineCap)shapeLayer.StrokeInfo.Style.StartCap;
                shape.StrokeLineJoin = (Svg.LineJoin)shapeLayer.StrokeInfo.Style.LineJoin;
                shape.StrokeMiterLimit = shapeLayer.StrokeInfo.Style.MiterLimit;

                element = shape;
            }

            if (element != null)
            {
                element.Transform = layer.Transform.Convert();

                element.Id = layer.Name;
            }

            return element;
        }

        public static Svg.Text ToSvg(Model.Text text)
        {
            var svgText = new Svg.Text
            {
                FontFamily = text.FontFamilyName,
                FontStretch = (Svg.FontStretch) text.FontStretch,
                FontWeight = (Svg.FontWeight) text.FontWeight,
                FontStyle = (Svg.FontStyle)text.FontStyle,
                FontSize = new Svg.Length(text.FontSize, Points),
                Text = text.Value,
                Y = text.Baseline
            };

            foreach (var format in text.Formats)
            {
                var span = new Svg.Span
                {
                    FontFamily = format.FontFamilyName ?? text.FontFamilyName,
                    FontStretch = (Svg.FontStretch?) format.FontStretch,
                    FontWeight = (Svg.FontWeight?) format.FontWeight,
                    FontStyle = (Svg.FontStyle?)format.FontStyle,
                    FontSize = format.FontSize != null ? new Svg.Length?(new Svg.Length(format.FontSize.Value, Points)) : null,
                    Text = text.Value?.Substring(format.Range.StartPosition, format.Range.Length),
                    Position = format.Range.StartPosition,

                    Fill = ToSvg(format.Fill),
                    FillOpacity = format.Fill?.Opacity ?? 1,
                    Stroke = ToSvg(format.Stroke),
                    StrokeOpacity = format.Stroke?.Opacity ?? 1
                };


                if (format.StrokeInfo != null)
                {
                    var dashes = format.StrokeInfo.Dashes.Select(f => f * format.StrokeInfo.Width).ToArray();

                    span.StrokeWidth = new Svg.Length(format.StrokeInfo.Width, Pixels);
                    span.StrokeDashArray = dashes.ToArray();
                    span.StrokeDashOffset = format.StrokeInfo.Style.DashOffset;
                    span.StrokeLineCap = (Svg.LineCap) format.StrokeInfo.Style.StartCap;
                    span.StrokeLineJoin = (Svg.LineJoin) format.StrokeInfo.Style.LineJoin;
                    span.StrokeMiterLimit = format.StrokeInfo.Style.MiterLimit;
                }

                svgText.Add(span);
            }

            return svgText;
        }

        public static Svg.Paint ToSvg(Model.BrushInfo brush)
        {
            if (brush is Model.SolidColorBrushInfo solidBrush)
            {
                return new Svg.SolidColor(
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