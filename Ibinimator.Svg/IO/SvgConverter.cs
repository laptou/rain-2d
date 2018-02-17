using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Geometry;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Model.Text;
using Ibinimator.Core.Utility;
using Ibinimator.Svg.Paint;
using Ibinimator.Svg.Shapes;

using Ellipse = Ibinimator.Svg.Shapes.Ellipse;
using GradientStop = Ibinimator.Core.Model.Paint.GradientStop;
using Group = Ibinimator.Core.Model.DocumentGraph.Group;
using Layer = Ibinimator.Core.Model.DocumentGraph.Layer;
using LineJoin = Ibinimator.Core.Model.LineJoin;
using Path = Ibinimator.Core.Model.DocumentGraph.Path;
using Rectangle = Ibinimator.Svg.Shapes.Rectangle;
using Text = Ibinimator.Svg.Shapes.Text;

namespace Ibinimator.Svg.IO
{
    public static class SvgConverter
    {
        public static Core.Document FromSvg(Svg.Document svgDocument)
        {
            var doc = new Core.Document
            {
                Root = new Group(),
                Bounds = new RectangleF(svgDocument.Viewbox.Left,
                                        svgDocument.Viewbox.Top,
                                        svgDocument.Viewbox.Width,
                                        svgDocument.Viewbox.Height)
            };

            foreach (var child in svgDocument.OfType<IGraphicalElement>().Select(FromSvg))
                doc.Root.Add(child, 0);

            return doc;
        }

        public static Layer FromSvg(IGraphicalElement element)
        {
            Layer layer = null;

            if (element is Svg.Structure.Group containerElement)
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
                        shape = new Core.Model.DocumentGraph.Ellipse
                        {
                            CenterX = ellipse.CenterX.To(LengthUnit.Pixels),
                            CenterY = ellipse.CenterY.To(LengthUnit.Pixels),
                            RadiusX = ellipse.RadiusX.To(LengthUnit.Pixels),
                            RadiusY = ellipse.RadiusY.To(LengthUnit.Pixels)
                        };

                        break;
                    case Line line:
                        var linePath = new Path();

                        linePath.Instructions.Add(
                            new MovePathInstruction(
                                line.X1.To(LengthUnit.Pixels),
                                line.Y1.To(LengthUnit.Pixels)));

                        linePath.Instructions.Add(
                            new LinePathInstruction(
                                line.X2.To(LengthUnit.Pixels),
                                line.Y2.To(LengthUnit.Pixels)));

                        shape = linePath;

                        break;
                    case Svg.Shapes.Path path:
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
                        shape = new Core.Model.DocumentGraph.Rectangle
                        {
                            X = rectangle.X.To(LengthUnit.Pixels),
                            Y = rectangle.Y.To(LengthUnit.Pixels),
                            Width = rectangle.Width.To(LengthUnit.Pixels),
                            Height = rectangle.Height.To(LengthUnit.Pixels)
                        };

                        break;
                    case Circle circle:
                        shape = new Core.Model.DocumentGraph.Ellipse
                        {
                            CenterX = circle.CenterX.To(LengthUnit.Pixels),
                            CenterY = circle.CenterY.To(LengthUnit.Pixels),
                            RadiusX = circle.Radius.To(LengthUnit.Pixels),
                            RadiusY = circle.Radius.To(LengthUnit.Pixels)
                        };

                        break;
                    case Text text:
                        shape = new Core.Model.DocumentGraph.Text
                        {
                            FontFamilyName = text.FontFamily ?? "Arial",
                            FontStretch = text.FontStretch ?? FontStretch.Normal,
                            FontWeight = text.FontWeight ?? FontWeight.Normal,
                            FontSize = text.FontSize?.To(LengthUnit.Points) ?? 12,
                            Value = text.Text
                        };

                        break;
                    default:

                        throw new ArgumentOutOfRangeException(nameof(shapeElement));
                }

                var dashes = shapeElement.StrokeDashArray
                                         .Select(
                                              f => f / shapeElement.StrokeWidth.To(
                                                       LengthUnit.Pixels))
                                         .ToArray();

                shape.Fill = FromSvg(shapeElement.Fill, shapeElement.FillOpacity);
                shape.Stroke = FromSvg(shapeElement.Stroke,
                                       shapeElement.StrokeOpacity,
                                       shapeElement.StrokeWidth.To(LengthUnit.Pixels),
                                       dashes,
                                       shapeElement.StrokeDashOffset,
                                       shapeElement.StrokeLineCap,
                                       shapeElement.StrokeLineJoin,
                                       shapeElement.StrokeMiterLimit);

                layer = shape as Layer;
            }

            if (layer != null)
            {
                layer.ApplyTransform(element.Transform);

                layer.Name = element.Id;
            }

            return layer;
        }

        public static IBrushInfo FromSvg(Paint.Paint paint, float opacity)
        {
            switch (paint)
            {
                case SolidColor solidColor:
                    var color = solidColor.Color;

                    return new SolidColorBrushInfo(
                        new Color(color.Red, color.Green, color.Blue, color.Alpha * opacity));

                case LinearGradient linearGradient:

                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(
                            linearGradient.Stops.Select(s => new GradientStop
                            {
                                Color = new Color(s.Color.Red,
                                                  s.Color.Green,
                                                  s.Color.Blue,
                                                  s.Color.Alpha * s.Opacity),
                                Offset = s.Offset.To(LengthUnit.Pixels, 1)
                            })),
                        StartPoint =
                            new Vector2(linearGradient.X1.To(LengthUnit.Pixels, 1),
                                        linearGradient.Y1.To(LengthUnit.Pixels, 1)),
                        EndPoint =
                            new Vector2(linearGradient.X2.To(LengthUnit.Pixels, 1),
                                        linearGradient.Y2.To(LengthUnit.Pixels, 1)),
                        Name = linearGradient.Id,
                        Transform = linearGradient.Transform,
                        SpreadMethod = linearGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = GradientBrushType.Linear
                    };

                case RadialGradient radialGradient:

                    return new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(
                            radialGradient.Stops.Select(s => new GradientStop
                            {
                                Color = new Color(s.Color.Red,
                                                  s.Color.Green,
                                                  s.Color.Blue,
                                                  s.Color.Alpha),
                                Offset = s.Offset.To(LengthUnit.Pixels, 1)
                            })),
                        StartPoint =
                            new Vector2(radialGradient.CenterX.To(LengthUnit.Pixels, 1),
                                        radialGradient.CenterY.To(LengthUnit.Pixels, 1)),
                        Focus =
                            new Vector2(radialGradient.FocusX.To(LengthUnit.Pixels, 1),
                                        radialGradient.FocusY.To(LengthUnit.Pixels, 1)),
                        EndPoint =
                            new Vector2(
                                radialGradient.CenterX.To(LengthUnit.Pixels, 1) +
                                radialGradient.Radius.To(LengthUnit.Pixels, 1),
                                radialGradient.CenterX.To(LengthUnit.Pixels, 1) +
                                radialGradient.Radius.To(LengthUnit.Pixels, 1)),
                        Name = radialGradient.Id,
                        Transform = radialGradient.Transform,
                        SpreadMethod = radialGradient.SpreadMethod,
                        Opacity = opacity,
                        GradientType = GradientBrushType.Radial
                    };
            }

            return null;
        }

        public static Svg.Document ToSvg(Core.Document doc)
        {
            var svgDoc = new Svg.Document();

            foreach (var child in doc.Root.SubLayers.Select(ToSvg))
                svgDoc.Insert(0, child);

            svgDoc.Viewbox = doc.Bounds;

            return svgDoc;
        }

        public static IElement ToSvg(ILayer layer)
        {
            IGraphicalElement element = null;

            if (layer is Group groupLayer)
            {
                var group = new Svg.Structure.Group();

                foreach (var child in groupLayer.SubLayers.Select(ToSvg))
                    group.Insert(0, child);

                element = group;
            }

            if (layer is IGeometricLayer shapeLayer)
            {
                IShapeElement shape = null;

                switch (shapeLayer)
                {
                    case Core.Model.DocumentGraph.Ellipse ellipse:
                        shape = new Ellipse
                        {
                            CenterX = new Length(ellipse.CenterX, LengthUnit.Pixels),
                            CenterY = new Length(ellipse.CenterY, LengthUnit.Pixels),
                            RadiusX = new Length(ellipse.RadiusX, LengthUnit.Pixels),
                            RadiusY = new Length(ellipse.RadiusY, LengthUnit.Pixels)
                        };

                        break;
                    case Path path:
                        var pathPath = new Svg.Shapes.Path();

                        pathPath.Data = path.Instructions.ToArray();

                        shape = pathPath;

                        break;
                    case Core.Model.DocumentGraph.Rectangle rectangle:
                        shape = new Rectangle
                        {
                            X = new Length(rectangle.X, LengthUnit.Pixels),
                            Y = new Length(rectangle.Y, LengthUnit.Pixels),
                            Width = new Length(rectangle.Width, LengthUnit.Pixels),
                            Height = new Length(rectangle.Height, LengthUnit.Pixels)
                        };

                        break;
                    case Core.Model.DocumentGraph.Text text:
                        shape = ToSvg(text);

                        break;
                    default:

                        throw new ArgumentOutOfRangeException(nameof(shapeLayer));
                }

                shape.Fill = ToSvg(shapeLayer.Fill);
                shape.FillOpacity = shapeLayer.Fill?.Opacity ?? 0;

                shape.Stroke = ToSvg(shapeLayer.Stroke?.Brush);
                shape.StrokeOpacity = shapeLayer.Stroke?.Brush?.Opacity ?? 0;

                var dashes = shapeLayer.Stroke.Dashes.Select(f => f * shapeLayer.Stroke.Width)
                                       .ToArray();

                shape.StrokeWidth = new Length(shapeLayer.Stroke.Width, LengthUnit.Pixels);
                shape.StrokeDashArray = dashes.ToArray();
                shape.StrokeDashOffset = shapeLayer.Stroke.DashOffset;
                shape.StrokeLineCap = shapeLayer.Stroke.LineCap;
                shape.StrokeLineJoin = shapeLayer.Stroke.LineJoin;
                shape.StrokeMiterLimit = shapeLayer.Stroke.MiterLimit;

                element = shape;
            }

            if (element != null)
            {
                element.Transform = layer.Transform;

                element.Id = layer.Name;
            }

            return element;
        }

        public static Text ToSvg(Core.Model.DocumentGraph.Text text)
        {
            var svgText = new Text
            {
                FontFamily = text.FontFamilyName,
                FontStretch = text.FontStretch,
                FontWeight = text.FontWeight,
                FontStyle = text.FontStyle,
                FontSize = new Length(text.FontSize, LengthUnit.Points),
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
                    FontSize =
                        format.FontSize != null
                            ? new Length?(new Length(format.FontSize.Value, LengthUnit.Points))
                            : null,
                    Text = text.Value?.Substring(format.Range.Index, format.Range.Length),
                    Position = format.Range.Index,
                    Fill = ToSvg(format.Fill),
                    FillOpacity = format.Fill?.Opacity ?? 1,
                    Stroke = ToSvg(format.Stroke?.Brush),
                    StrokeOpacity = format.Stroke?.Brush?.Opacity ?? 1
                };


                if (format.Stroke != null)
                {
                    var dashes = format.Stroke.Dashes.Select(f => f * format.Stroke.Width)
                                       .ToArray();

                    span.StrokeWidth = new Length(format.Stroke.Width, LengthUnit.Pixels);
                    span.StrokeDashArray = dashes.ToArray();
                    span.StrokeDashOffset = format.Stroke.DashOffset;
                    span.StrokeLineCap = format.Stroke.LineCap;
                    span.StrokeLineJoin = format.Stroke.LineJoin;
                    span.StrokeMiterLimit = format.Stroke.MiterLimit;
                }

                svgText.Add(span);
            }

            return svgText;
        }

        public static Paint.Paint ToSvg(IBrushInfo brush)
        {
            if (brush is SolidColorBrushInfo solidBrush)
                return new SolidColor(solidBrush.Color);

            return null;
        }

        private static IPenInfo FromSvg(
            Paint.Paint paint, float opacity, float width, float[] dashes, float dashOffset,
            LineCap lineCap, LineJoin lineJoin, float miterLimit)
        {
            var stroke = new PenInfo
            {
                Brush = FromSvg(paint, opacity),
                Width = width,
                LineCap = lineCap,
                LineJoin = lineJoin,
                DashOffset = dashOffset,
                MiterLimit = miterLimit
            };

            stroke.Dashes.AddItems(dashes);

            return stroke;
        }
    }
}