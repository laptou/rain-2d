using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Core.Utility;
using Rain.Formatter.Svg.Paint;
using Rain.Formatter.Svg.Shapes;
using Rain.Formatter.Svg.Structure;

using Document = Rain.Formatter.Svg.Structure.Document;
using Ellipse = Rain.Formatter.Svg.Shapes.Ellipse;
using GradientStop = Rain.Core.Model.Paint.GradientStop;
using Group = Rain.Formatter.Svg.Structure.Group;
using Path = Rain.Formatter.Svg.Shapes.Path;
using Rectangle = Rain.Formatter.Svg.Shapes.Rectangle;
using Text = Rain.Formatter.Svg.Shapes.Text;

namespace Rain.Formatter.Svg.IO
{
    public static class SvgReader
    {
        public static Core.Model.DocumentGraph.Document FromSvg(Document svgDocument)
        {
            var doc = new Core.Model.DocumentGraph.Document
            {
                Root = new Core.Model.DocumentGraph.Group(),
                Bounds = new RectangleF(svgDocument.Viewbox.Left,
                                        svgDocument.Viewbox.Top,
                                        svgDocument.Viewbox.Width,
                                        svgDocument.Viewbox.Height)
            };

            foreach (var child in svgDocument
                                 .OfType<IGraphicalElement>()
                                 .Select(g => FromSvg(svgDocument, g)))
                doc.Root.Add(child, 0);

            return doc;
        }

        private static Layer FromSvg(Document svgDocument, IGraphicalElement element)
        {
            Layer layer = null;

            if (element is Group containerElement)
            {
                var group = new Core.Model.DocumentGraph.Group();

                foreach (var child in containerElement
                                     .OfType<IGraphicalElement>()
                                     .Select(g => FromSvg(svgDocument, g)))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is IShapeElement shapeElement)
            {
                IGeometricLayer shape;

                // ReSharper disable RedundantNameQualifier
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
                        var linePath = new Core.Model.DocumentGraph.Path();

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
                    case Path path:
                        var pathPath = new Core.Model.DocumentGraph.Path();

                        pathPath.Instructions.AddItems(path.Data);

                        shape = pathPath;

                        break;
                    case Polygon polygon:
                        var polygonPath = new Core.Model.DocumentGraph.Path();

                        polygonPath.Instructions.AddItems(
                            polygon.Points.Select(v => new LinePathInstruction(v)));

                        polygonPath.Instructions.Add(new ClosePathInstruction(false));

                        shape = polygonPath;

                        break;
                    case Polyline polyline:
                        var polylinePath = new Core.Model.DocumentGraph.Path();

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
                            TextStyle = new TextInfo
                            {
                                FontFamily = text.FontFamily ?? "Arial",
                                FontStretch = text.FontStretch ?? FontStretch.Normal,
                                FontWeight = text.FontWeight ?? FontWeight.Normal,
                                FontSize = text.FontSize?.To(LengthUnit.Points) ?? 12
                            },
                            Value = text.Text
                        };

                        break;
                    default:

                        throw new ArgumentOutOfRangeException(nameof(shapeElement));
                }

                // ReSharper restore RedundantNameQualifier

                var dashes = shapeElement.StrokeDashArray
                                         .Select(
                                              f => f / shapeElement.StrokeWidth.To(
                                                       LengthUnit.Pixels))
                                         .ToArray();

                shape.Fill = FromSvg(svgDocument, shapeElement.Fill, shapeElement.FillOpacity);
                shape.Stroke = FromSvg(svgDocument,
                                       shapeElement.Stroke,
                                       shapeElement.StrokeOpacity,
                                       shapeElement.StrokeWidth.To(LengthUnit.Pixels),
                                       dashes,
                                       shapeElement.StrokeDashOffset,
                                       shapeElement.StrokeLineCap,
                                       shapeElement.StrokeLineJoin,
                                       shapeElement.StrokeMiterLimit);

                layer = (Layer) shape;
            }

            if (layer != null)
            {
                layer.ApplyTransform(element.Transform);

                layer.Name = element.Name ?? element.Id;
            }

            return layer;
        }

        private static IBrushInfo FromSvg(
            Document svgDocument, Paint.Paint paint, float opacity)
        {
            switch (paint)
            {
                case SolidColorPaint solidColor:
                    var color = solidColor.Color;

                    return new SolidColorBrushInfo(
                        new Color(color.Red, color.Green, color.Blue, color.Alpha * opacity));

                case LinearGradientPaint linearGradient:

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
                        Name = linearGradient.Name ?? linearGradient.Id,
                        Transform = linearGradient.Transform,
                        SpreadMethod = linearGradient.SpreadMethod,
                        Opacity = opacity,
                        Type = GradientBrushType.Linear
                    };

                case RadialGradientPaint radialGradient:

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
                        Name = radialGradient.Name ?? radialGradient.Id,
                        Transform = radialGradient.Transform,
                        SpreadMethod = radialGradient.SpreadMethod,
                        Opacity = opacity,
                        Type = GradientBrushType.Radial
                    };

                case ReferencePaint reference:

                    return FromSvg(svgDocument,
                                   svgDocument.Defs[reference.Reference.Id] as Paint.Paint,
                                   1);
            }

            return null;
        }

        private static IPenInfo FromSvg(
            Document svgDocument, Paint.Paint paint, float opacity, float width,
            IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit)
        {
            var stroke = new PenInfo
            {
                Brush = FromSvg(svgDocument, paint, opacity),
                Width = width,
                LineCap = lineCap,
                LineJoin = lineJoin,
                DashOffset = dashOffset,
                MiterLimit = miterLimit
            };

            stroke.Dashes.AddItems(dashes.Select(d => d / width));

            return stroke;
        }
    }
}