using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Geometry;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Text;
using Ibinimator.Core.Utility;
using Ibinimator.Svg.Structure;

using DG = Ibinimator.Core.Model.DocumentGraph;
using DGPaint = Ibinimator.Core.Model.Paint;
using SVG = Ibinimator.Svg.Shapes;
using SVGPaint = Ibinimator.Svg.Paint;

namespace Ibinimator.Svg.IO
{
    public static class SvgReader
    {
        public static DG.Document FromSvg(Document svgDocument)
        {
            var doc = new DG.Document
            {
                Root = new DG.Group(),
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

        private static DG.Layer FromSvg(Document svgDocument, IGraphicalElement element)
        {
            DG.Layer layer = null;

            if (element is Group containerElement)
            {
                var group = new DG.Group();

                foreach (var child in containerElement
                                     .OfType<IGraphicalElement>()
                                     .Select(g => FromSvg(svgDocument, g)))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is SVG.IShapeElement shapeElement)
            {
                DG.IGeometricLayer shape;

                // ReSharper disable RedundantNameQualifier
                switch (shapeElement)
                {
                    case SVG.Ellipse ellipse:
                        shape = new DG.Ellipse
                        {
                            CenterX = ellipse.CenterX.To(LengthUnit.Pixels),
                            CenterY = ellipse.CenterY.To(LengthUnit.Pixels),
                            RadiusX = ellipse.RadiusX.To(LengthUnit.Pixels),
                            RadiusY = ellipse.RadiusY.To(LengthUnit.Pixels)
                        };

                        break;
                    case SVG.Line line:
                        var linePath = new DG.Path();

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
                    case SVG.Path path:
                        var pathPath = new DG.Path();

                        pathPath.Instructions.AddItems(path.Data);

                        shape = pathPath;

                        break;
                    case SVG.Polygon polygon:
                        var polygonPath = new DG.Path();

                        polygonPath.Instructions.AddItems(
                            polygon.Points.Select(v => new LinePathInstruction(v)));

                        polygonPath.Instructions.Add(new ClosePathInstruction(false));

                        shape = polygonPath;

                        break;
                    case SVG.Polyline polyline:
                        var polylinePath = new DG.Path();

                        polylinePath.Instructions.AddItems(
                            polyline.Points.Select(v => new LinePathInstruction(v)));

                        polylinePath.Instructions.Add(new ClosePathInstruction(true));

                        shape = polylinePath;

                        break;
                    case SVG.Rectangle rectangle:
                        shape = new DG.Rectangle
                        {
                            X = rectangle.X.To(LengthUnit.Pixels),
                            Y = rectangle.Y.To(LengthUnit.Pixels),
                            Width = rectangle.Width.To(LengthUnit.Pixels),
                            Height = rectangle.Height.To(LengthUnit.Pixels)
                        };

                        break;
                    case SVG.Circle circle:
                        shape = new DG.Ellipse
                        {
                            CenterX = circle.CenterX.To(LengthUnit.Pixels),
                            CenterY = circle.CenterY.To(LengthUnit.Pixels),
                            RadiusX = circle.Radius.To(LengthUnit.Pixels),
                            RadiusY = circle.Radius.To(LengthUnit.Pixels)
                        };

                        break;
                    case SVG.Text text:
                        shape = new DG.Text
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

                layer = (DG.Layer) shape;
            }

            if (layer != null)
            {
                layer.ApplyTransform(element.Transform);

                layer.Name = element.Name ?? element.Id;
            }

            return layer;
        }

        private static DGPaint.IBrushInfo FromSvg(
            Document svgDocument, SVGPaint.Paint paint, float opacity)
        {
            switch (paint)
            {
                case SVGPaint.SolidColorPaint solidColor:
                    var color = solidColor.Color;

                    return new DGPaint.SolidColorBrushInfo(
                        new Color(color.Red, color.Green, color.Blue, color.Alpha * opacity));

                case SVGPaint.LinearGradientPaint linearGradient:

                    return new DGPaint.GradientBrushInfo
                    {
                        Stops = new ObservableList<DGPaint.GradientStop>(
                            linearGradient.Stops.Select(s => new DGPaint.GradientStop
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
                        Type = DG.GradientBrushType.Linear
                    };

                case SVGPaint.RadialGradientPaint radialGradient:

                    return new DGPaint.GradientBrushInfo
                    {
                        Stops = new ObservableList<DGPaint.GradientStop>(
                            radialGradient.Stops.Select(s => new DGPaint.GradientStop
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
                        Type = DG.GradientBrushType.Radial
                    };

                case SVGPaint.ReferencePaint reference:

                    return FromSvg(svgDocument,
                                   svgDocument.Defs[reference.Reference.Id] as SVGPaint.Paint,
                                   1);
            }

            return null;
        }

        private static DGPaint.IPenInfo FromSvg(
            Document svgDocument, SVGPaint.Paint paint, float opacity, float width,
            IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit)
        {
            var stroke = new DG.PenInfo
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