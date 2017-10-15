using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using SharpDX.Direct2D1;
using static Ibinimator.Svg.LengthUnit;
using ArcPathNode = Ibinimator.Renderer.Model.ArcPathNode;
using CloseNode = Ibinimator.Renderer.Model.CloseNode;
using Color = Ibinimator.Core.Color;
using CubicPathNode = Ibinimator.Renderer.Model.CubicPathNode;
using Ellipse = Ibinimator.Renderer.Model.Ellipse;
using FontStretch = Ibinimator.Renderer.FontStretch;
using FontWeight = Ibinimator.Renderer.FontWeight;
using GradientStop = SharpDX.Direct2D1.GradientStop;
using Layer = Ibinimator.Renderer.Model.Layer;
using LineJoin = SharpDX.Direct2D1.LineJoin;
using Path = Ibinimator.Renderer.Model.Path;
using PathNode = Ibinimator.Renderer.Model.PathNode;
using QuadraticPathNode = Ibinimator.Renderer.Model.QuadraticPathNode;
using Rectangle = Ibinimator.Renderer.Model.Rectangle;
using Text = Ibinimator.Renderer.Model.Text;

namespace Ibinimator.Utility
{
    public static class SvgConverter
    {
        public static Document FromSvg(Svg.Document svgDocument)
        {
            var doc = new Document
            {
                Root = new Group(),
                Bounds = new RectangleF(
                    svgDocument.Viewbox.X, svgDocument.Viewbox.Y,
                    svgDocument.Viewbox.Width, svgDocument.Viewbox.Height)
            };

            foreach (var child in svgDocument.OfType<Svg.IGraphicalElement>().Select(FromSvg))
                doc.Root.Add(child, 0);

            return doc;
        }

        public static Layer FromSvg(Svg.IGraphicalElement element)
        {
            Layer layer = null;

            if (element is Svg.IContainerElement containerElement)
            {
                var group = new Group();

                foreach (var child in containerElement.OfType<Svg.IGraphicalElement>().Select(FromSvg))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is Svg.IShapeElement shapeElement)
            {
                IGeometricLayer shape = null;

                switch (shapeElement)
                {
                    case Svg.Ellipse ellipse:
                        shape = new Ellipse
                        {
                            CenterX = ellipse.CenterX.To(Pixels),
                            CenterY = ellipse.CenterY.To(Pixels),
                            RadiusX = ellipse.RadiusX.To(Pixels),
                            RadiusY = ellipse.RadiusY.To(Pixels)
                        };
                        break;
                    case Svg.Line line:
                        var linePath = new Path();

                        linePath.Nodes.Add(new PathNode
                        {
                            X = line.X1.To(Pixels),
                            Y = line.Y1.To(Pixels)
                        });

                        linePath.Nodes.Add(new PathNode
                        {
                            X = line.X2.To(Pixels),
                            Y = line.Y2.To(Pixels)
                        });

                        shape = linePath;
                        break;
                    case Svg.Path path:
                        var pathPath = new Path();

                        pathPath.Nodes.AddItems(path.Data.Select(pathNode =>
                        {
                            switch (pathNode)
                            {
                                case Svg.ArcPathNode arcPathNode:
                                    return new ArcPathNode
                                    {
                                        Clockwise = arcPathNode.Clockwise,
                                        LargeArc = arcPathNode.LargeArc,
                                        Position = arcPathNode.Position,
                                        RadiusX = arcPathNode.RadiusX,
                                        RadiusY = arcPathNode.RadiusY,
                                        Rotation = arcPathNode.Rotation
                                    };
                                case Svg.CloseNode closeNode:
                                    return new CloseNode
                                    {
                                        Open = closeNode.Open
                                    };
                                case Svg.CubicPathNode cubicPathNode:
                                    return new CubicPathNode
                                    {
                                        Control1 = cubicPathNode.Control1,
                                        Control2 = cubicPathNode.Control2,
                                        Position = cubicPathNode.Position
                                    };
                                case Svg.QuadraticPathNode quadraticPathNode:
                                    return new QuadraticPathNode
                                    {
                                        Control = quadraticPathNode.Control,
                                        Position = quadraticPathNode.Position
                                    };
                                default:
                                    return new PathNode
                                    {
                                        Position = pathNode.Position
                                    };
                            }
                        }));

                        shape = pathPath;
                        break;
                    case Svg.Polygon polygon:
                        var polygonPath = new Path();

                        polygonPath.Nodes.AddItems(
                            polygon.Points.Select(v => new PathNode
                            {
                                Position = v
                            }));

                        polygonPath.Nodes.Add(new CloseNode {Open = false});

                        shape = polygonPath;
                        break;
                    case Svg.Polyline polyline:
                        var polylinePath = new Path();

                        polylinePath.Nodes.AddItems(
                            polyline.Points.Select(v => new PathNode
                            {
                                Position = v
                            }));

                        polylinePath.Nodes.Add(new CloseNode {Open = true});

                        shape = polylinePath;
                        break;
                    case Svg.Rectangle rectangle:
                        shape = new Rectangle
                        {
                            X = rectangle.X.To(Pixels),
                            Y = rectangle.Y.To(Pixels),
                            Width = rectangle.Width.To(Pixels),
                            Height = rectangle.Height.To(Pixels)
                        };
                        break;
                    case Svg.Circle circle:
                        shape = new Ellipse
                        {
                            CenterX = circle.CenterX.To(Pixels),
                            CenterY = circle.CenterY.To(Pixels),
                            RadiusX = circle.Radius.To(Pixels),
                            RadiusY = circle.Radius.To(Pixels)
                        };
                        break;
                    case Svg.Text text:
                        shape = new Text
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

                var dashes = shapeElement.StrokeDashArray.Select(f => f / shapeElement.StrokeWidth.To(Pixels)).ToArray();

                shape.Fill = FromSvg(shapeElement.Fill, shapeElement.FillOpacity);
                shape.Stroke = FromSvg(
                    shapeElement.Stroke, shapeElement.StrokeOpacity, 
                    shapeElement.StrokeWidth.To(Pixels), dashes, shapeElement.StrokeDashOffset,
                    shapeElement.StrokeLineCap, shapeElement.StrokeLineJoin, shapeElement.StrokeMiterLimit);

                layer = shape as Layer;
            }

            if (layer != null)
            {

                (layer.Scale, layer.Rotation, layer.Position, layer.Shear) =
                    element.Transform.Decompose();

                layer.Name = element.Id;
            }

            return layer;
        }

        private static PenInfo FromSvg(Svg.Paint paint, float opacity,
            float width, float[] dashes, float dashOffset, Svg.LineCap lineCap, 
            Svg.LineJoin lineJoin, float miterLimit)
        {
            var stroke = new PenInfo
            {
                Width = width,
                Style = new StrokeStyleProperties1
                {
                    DashStyle = dashes.Any() ? DashStyle.Custom : DashStyle.Solid,
                    DashCap = (CapStyle)lineCap,
                    StartCap = (CapStyle)lineCap,
                    EndCap = (CapStyle)lineCap,
                    LineJoin = (LineJoin)lineJoin,
                    DashOffset = dashOffset,
                    MiterLimit = miterLimit
                }
            };

            stroke.Dashes.AddItems(dashes);

            return stroke;
        }

        public static BrushInfo FromSvg(Svg.Paint paint, float opacity)
        {
            switch (paint)
            {
                case Svg.SolidColor solidColor:
                    var color = solidColor.Color;

                    return new SolidColorBrushInfo(
                        new Color(
                            color.Red,
                            color.Green,
                            color.Blue,
                            color.Alpha * opacity));

                case Svg.LinearGradient linearGradient:
                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<Renderer.GradientStop>(
                            linearGradient.Stops.Select(s => new Renderer.GradientStop
                        {
                            Color = new Color(
                                s.Color.Red,
                                s.Color.Green,
                                s.Color.Blue,
                                s.Color.Alpha),
                            Offset = s.Offset.To(Pixels, 1)
                        })),
                        StartPoint = new Vector2(linearGradient.X1.To(Pixels, 1), linearGradient.Y1.To(Pixels, 1)),
                        EndPoint = new Vector2(linearGradient.X2.To(Pixels, 1), linearGradient.Y2.To(Pixels, 1)),
                        Name = linearGradient.Id,
                        Transform = linearGradient.Transform,
                        ExtendMode = (ExtendMode)linearGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = GradientBrushType.Linear
                    };

                case Svg.RadialGradient radialGradient:
                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<Renderer.GradientStop>(
                            radialGradient.Stops.Select(s => new Renderer.GradientStop
                        {
                            Color = new Color(
                                s.Color.Red,
                                s.Color.Green,
                                s.Color.Blue,
                                s.Color.Alpha),
                            Offset = s.Offset.To(Pixels, 1)
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
                        Transform = radialGradient.Transform,
                        ExtendMode = (ExtendMode)radialGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = GradientBrushType.Radial
                    };
            }

            return null;
        }
        

        public static Svg.Document ToSvg(Document doc)
        {
            var svgDoc = new Svg.Document();

            foreach (var child in doc.Root.SubLayers.Select(ToSvg))
                svgDoc.Insert(0, child);

            var bounds = doc.Bounds;

            svgDoc.Viewbox = new System.Drawing.RectangleF(
                bounds.Left, bounds.Top, bounds.Width, bounds.Height);

            return svgDoc;
        }

        public static Svg.IElement ToSvg(Layer layer)
        {
            Svg.IGraphicalElement element = null;

            if (layer is Group groupLayer)
            {
                var group = new Svg.Group();

                foreach (var child in groupLayer.SubLayers.Select(ToSvg))
                    group.Insert(0, child);

                element = group;
            }

            if (layer is IGeometricLayer shapeLayer)
            {
                Svg.IShapeElement shape = null;

                switch (shapeLayer)
                {
                    case Ellipse ellipse:
                        shape = new Svg.Ellipse
                        {
                            CenterX = new Svg.Length(ellipse.CenterX, Pixels),
                            CenterY = new Svg.Length(ellipse.CenterY, Pixels),
                            RadiusX = new Svg.Length(ellipse.RadiusX, Pixels),
                            RadiusY = new Svg.Length(ellipse.RadiusY, Pixels)
                        };
                        break;
                    case Path path:
                        var pathPath = new Svg.Path();

                        pathPath.Data = path.Nodes.Select(pathNode =>
                        {
                            switch (pathNode)
                            {
                                case ArcPathNode arcPathNode:
                                    return new Svg.ArcPathNode
                                    {
                                        Clockwise = arcPathNode.Clockwise,
                                        LargeArc = arcPathNode.LargeArc,
                                        Position = arcPathNode.Position,
                                        RadiusX = arcPathNode.RadiusX,
                                        RadiusY = arcPathNode.RadiusY,
                                        Rotation = arcPathNode.Rotation
                                    };
                                case CloseNode closeNode:
                                    return new Svg.CloseNode
                                    {
                                        Open = closeNode.Open
                                    };
                                case CubicPathNode cubicPathNode:
                                    return new Svg.CubicPathNode
                                    {
                                        Control1 = cubicPathNode.Control1,
                                        Control2 = cubicPathNode.Control2,
                                        Position = cubicPathNode.Position
                                    };
                                case QuadraticPathNode quadraticPathNode:
                                    return new Svg.QuadraticPathNode
                                    {
                                        Control = quadraticPathNode.Control,
                                        Position = quadraticPathNode.Position
                                    };
                                default:
                                    return new Svg.PathNode
                                    {
                                        Position = pathNode.Position
                                    };
                            }
                        }).ToArray();

                        shape = pathPath;
                        break;
                    case Rectangle rectangle:
                        shape = new Svg.Rectangle
                        {
                            X = new Svg.Length(rectangle.X, Pixels),
                            Y = new Svg.Length(rectangle.Y, Pixels),
                            Width = new Svg.Length(rectangle.Width, Pixels),
                            Height = new Svg.Length(rectangle.Height, Pixels)
                        };
                        break;
                    case Text text:
                        shape = ToSvg(text);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(shapeLayer));
                }

                shape.Fill = ToSvg(shapeLayer.Fill);
                shape.FillOpacity = shapeLayer.Fill?.Opacity ?? 0;

                shape.Stroke = ToSvg(shapeLayer.Stroke?.Brush);
                shape.StrokeOpacity = shapeLayer.Stroke?.Brush?.Opacity ?? 0;

                var dashes = shapeLayer.Stroke.Dashes.Select(f => f * shapeLayer.Stroke.Width).ToArray();

                shape.StrokeWidth = new Svg.Length(shapeLayer.Stroke.Width, Pixels);
                shape.StrokeDashArray = dashes.ToArray();
                shape.StrokeDashOffset = shapeLayer.Stroke.Style.DashOffset;
                shape.StrokeLineCap = (Svg.LineCap)shapeLayer.Stroke.Style.StartCap;
                shape.StrokeLineJoin = (Svg.LineJoin)shapeLayer.Stroke.Style.LineJoin;
                shape.StrokeMiterLimit = shapeLayer.Stroke.Style.MiterLimit;

                element = shape;
            }

            if (element != null)
            {
                element.Transform = layer.Transform;

                element.Id = layer.Name;
            }

            return element;
        }

        public static Svg.Text ToSvg(Text text)
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
                    Text = text.Value?.Substring(format.Range.Index, format.Range.Length),
                    Position = format.Range.Length,

                    Fill = ToSvg(format.Fill),
                    FillOpacity = format.Fill?.Opacity ?? 1,
                    Stroke = ToSvg(format.Stroke?.Brush),
                    StrokeOpacity = format.Stroke?.Brush?.Opacity ?? 1
                };


                if (format.Stroke != null)
                {
                    var dashes = format.Stroke.Dashes.Select(f => f * format.Stroke.Width).ToArray();

                    span.StrokeWidth = new Svg.Length(format.Stroke.Width, Pixels);
                    span.StrokeDashArray = dashes.ToArray();
                    span.StrokeDashOffset = format.Stroke.Style.DashOffset;
                    span.StrokeLineCap = (Svg.LineCap) format.Stroke.Style.StartCap;
                    span.StrokeLineJoin = (Svg.LineJoin) format.Stroke.Style.LineJoin;
                    span.StrokeMiterLimit = format.Stroke.Style.MiterLimit;
                }

                svgText.Add(span);
            }

            return svgText;
        }

        public static Svg.Paint ToSvg(BrushInfo brush)
        {
            if (brush is SolidColorBrushInfo solidBrush)
            {
                return new Svg.SolidColor(
                    new Svg.Color(
                        solidBrush.Color.R,
                        solidBrush.Color.G,
                        solidBrush.Color.B,
                        solidBrush.Color.A
                        ));
            }

            return null;
        }
    }
}