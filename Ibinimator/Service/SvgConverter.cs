using System;
using System.Collections.Generic;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Svg;
using SharpDX.Direct2D1;
using static Ibinimator.Core.Model.LengthUnit;
using Color = Ibinimator.Core.Model.Color;
using Document = Ibinimator.Renderer.Model.Document;
using Ellipse = Ibinimator.Svg.Ellipse;
using GradientStop = Ibinimator.Renderer.GradientStop;
using Group = Ibinimator.Renderer.Model.Group;
using Layer = Ibinimator.Renderer.Model.Layer;
using LineCap = Ibinimator.Svg.LineCap;
using LineJoin = Ibinimator.Svg.LineJoin;
using Path = Ibinimator.Renderer.Model.Path;
using Rectangle = Ibinimator.Svg.Rectangle;
using Text = Ibinimator.Svg.Text;

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

            foreach (var child in svgDocument.OfType<IGraphicalElement>().Select(FromSvg))
                doc.Root.Add(child, 0);

            return doc;
        }

        public static Layer FromSvg(IGraphicalElement element)
        {
            Layer layer = null;

            if (element is IContainerElement containerElement)
            {
                var group = new Group();

                foreach (var child in containerElement.OfType<IGraphicalElement>().Select(FromSvg))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is IShapeElement shapeElement)
            {
                IGeometricLayer shape = null;

                switch (shapeElement)
                {
                    case Ellipse ellipse:
                        shape = new Renderer.Model.Ellipse
                        {
                            CenterX = ellipse.CenterX.To(Pixels),
                            CenterY = ellipse.CenterY.To(Pixels),
                            RadiusX = ellipse.RadiusX.To(Pixels),
                            RadiusY = ellipse.RadiusY.To(Pixels)
                        };
                        break;
                    case Line line:
                        var linePath = new Path();

                        linePath.Instructions.Add(
                            new MovePathInstruction(
                                line.X1.To(Pixels),
                                line.Y1.To(Pixels)));

                        linePath.Instructions.Add(
                            new LinePathInstruction(
                                line.X2.To(Pixels),
                                line.Y2.To(Pixels)));

                        shape = linePath;
                        break;
                    case Svg.Path path:
                        var pathPath = new Path();

                        pathPath.Instructions.AddItems(path.Data);

                        shape = pathPath;
                        break;
                    case Polygon polygon:
                        var polygonPath = new Path();

                        polygonPath.Instructions.AddItems(
                            polygon.Points.Select(v => new LinePathInstruction(v)));

                        polygonPath.Instructions.Add(new ClosePathInstruction(false));

                        shape = polygonPath;
                        break;
                    case Polyline polyline:
                        var polylinePath = new Path();

                        polylinePath.Instructions.AddItems(
                            polyline.Points.Select(v => new LinePathInstruction(v)));

                        polylinePath.Instructions.Add(new ClosePathInstruction(true));

                        shape = polylinePath;
                        break;
                    case Rectangle rectangle:
                        shape = new Renderer.Model.Rectangle
                        {
                            X = rectangle.X.To(Pixels),
                            Y = rectangle.Y.To(Pixels),
                            Width = rectangle.Width.To(Pixels),
                            Height = rectangle.Height.To(Pixels)
                        };
                        break;
                    case Circle circle:
                        shape = new Renderer.Model.Ellipse
                        {
                            CenterX = circle.CenterX.To(Pixels),
                            CenterY = circle.CenterY.To(Pixels),
                            RadiusX = circle.Radius.To(Pixels),
                            RadiusY = circle.Radius.To(Pixels)
                        };
                        break;
                    case Text text:
                        shape = new Renderer.Model.Text
                        {
                            FontFamilyName = text.FontFamily ?? "Arial",
                            FontStretch = text.FontStretch ?? FontStretch.Normal,
                            FontWeight = text.FontWeight ?? FontWeight.Normal,
                            FontSize = text.FontSize?.To(Points) ?? 12,
                            Value = text.Text
                        };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(shapeElement));
                }

                var dashes = shapeElement.StrokeDashArray.Select(f => f / shapeElement.StrokeWidth.To(Pixels))
                    .ToArray();

                shape.Fill = FromSvg(shapeElement.Fill, shapeElement.FillOpacity);
                shape.Stroke = FromSvg(
                    shapeElement.Stroke, shapeElement.StrokeOpacity,
                    shapeElement.StrokeWidth.To(Pixels), dashes, shapeElement.StrokeDashOffset,
                    shapeElement.StrokeLineCap, shapeElement.StrokeLineJoin, shapeElement.StrokeMiterLimit);

                layer = shape as Layer;
            }

            if (layer != null)
            {
                layer.ApplyTransform(element.Transform);

                layer.Name = element.Id;
            }

            return layer;
        }

        public static BrushInfo FromSvg(Paint paint, float opacity)
        {
            switch (paint)
            {
                case SolidColor solidColor:
                    var color = solidColor.Color;

                    return new SolidColorBrushInfo(
                        new Color(
                            color.Red,
                            color.Green,
                            color.Blue,
                            color.Alpha * opacity));

                case LinearGradient linearGradient:
                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(
                            linearGradient.Stops.Select(s => new GradientStop
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
                        ExtendMode = (ExtendMode) linearGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = GradientBrushType.Linear
                    };

                case RadialGradient radialGradient:
                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(
                            radialGradient.Stops.Select(s => new GradientStop
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
                        ExtendMode = (ExtendMode) radialGradient.SpreadMethod,
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

        public static IElement ToSvg(Layer layer)
        {
            IGraphicalElement element = null;

            if (layer is Group groupLayer)
            {
                var group = new Svg.Group();

                foreach (var child in groupLayer.SubLayers.Select(ToSvg))
                    group.Insert(0, child);

                element = group;
            }

            if (layer is IGeometricLayer shapeLayer)
            {
                IShapeElement shape = null;

                switch (shapeLayer)
                {
                    case Renderer.Model.Ellipse ellipse:
                        shape = new Ellipse
                        {
                            CenterX = new Length(ellipse.CenterX, Pixels),
                            CenterY = new Length(ellipse.CenterY, Pixels),
                            RadiusX = new Length(ellipse.RadiusX, Pixels),
                            RadiusY = new Length(ellipse.RadiusY, Pixels)
                        };
                        break;
                    case Path path:
                        var pathPath = new Svg.Path();

                        pathPath.Data = path.Instructions.ToArray();

                        shape = pathPath;
                        break;
                    case Renderer.Model.Rectangle rectangle:
                        shape = new Rectangle
                        {
                            X = new Length(rectangle.X, Pixels),
                            Y = new Length(rectangle.Y, Pixels),
                            Width = new Length(rectangle.Width, Pixels),
                            Height = new Length(rectangle.Height, Pixels)
                        };
                        break;
                    case Renderer.Model.Text text:
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

                shape.StrokeWidth = new Length(shapeLayer.Stroke.Width, Pixels);
                shape.StrokeDashArray = dashes.ToArray();
                shape.StrokeDashOffset = shapeLayer.Stroke.Style.DashOffset;
                shape.StrokeLineCap = (LineCap) shapeLayer.Stroke.Style.StartCap;
                shape.StrokeLineJoin = (LineJoin) shapeLayer.Stroke.Style.LineJoin;
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

        public static Text ToSvg(Renderer.Model.Text text)
        {
            var svgText = new Text
            {
                FontFamily = text.FontFamilyName,
                FontStretch = text.FontStretch,
                FontWeight = text.FontWeight,
                FontStyle = text.FontStyle,
                FontSize = new Length(text.FontSize, Points),
                Text = text.Value,
                Y = text.Baseline
            };

            foreach (var format in text.Formats)
            {
                var span = new Span
                {
                    FontFamily = format.FontFamilyName ?? text.FontFamilyName,
                    FontStretch = format.FontStretch,
                    FontWeight = format.FontWeight,
                    FontStyle = format.FontStyle,
                    FontSize = format.FontSize != null ? new Length?(new Length(format.FontSize.Value, Points)) : null,
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

                    span.StrokeWidth = new Length(format.Stroke.Width, Pixels);
                    span.StrokeDashArray = dashes.ToArray();
                    span.StrokeDashOffset = format.Stroke.Style.DashOffset;
                    span.StrokeLineCap = (LineCap) format.Stroke.Style.StartCap;
                    span.StrokeLineJoin = (LineJoin) format.Stroke.Style.LineJoin;
                    span.StrokeMiterLimit = format.Stroke.Style.MiterLimit;
                }

                svgText.Add(span);
            }

            return svgText;
        }

        public static Paint ToSvg(BrushInfo brush)
        {
            if (brush is SolidColorBrushInfo solidBrush)
                return new SolidColor(
                    new Svg.Color(
                        solidBrush.Color.R,
                        solidBrush.Color.G,
                        solidBrush.Color.B,
                        solidBrush.Color.A
                    ));

            return null;
        }

        private static PenInfo FromSvg(Paint paint, float opacity,
            float width, float[] dashes, float dashOffset, LineCap lineCap,
            LineJoin lineJoin, float miterLimit)
        {
            var stroke = new PenInfo
            {
                Brush = FromSvg(paint, opacity),
                Width = width,
                Style = new StrokeStyleProperties1
                {
                    DashStyle = dashes.Any() ? DashStyle.Custom : DashStyle.Solid,
                    DashCap = (CapStyle) lineCap,
                    StartCap = (CapStyle) lineCap,
                    EndCap = (CapStyle) lineCap,
                    LineJoin = (SharpDX.Direct2D1.LineJoin) lineJoin,
                    DashOffset = dashOffset,
                    MiterLimit = miterLimit
                }
            };

            stroke.Dashes.AddItems(dashes);

            return stroke;
        }
    }
}