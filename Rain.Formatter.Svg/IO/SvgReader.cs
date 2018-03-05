using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;

using DG = Rain.Core.Model.DocumentGraph;

using Rain.Core.Model.Geometry;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Core.Utility;

using SVG = Rain.Formatter.Svg;

using Rain.Formatter.Svg.Paint;
using Rain.Formatter.Svg.Shapes;
using Rain.Formatter.Svg.Structure;
using Rain.Formatter.Svg.Utilities;

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

            var resolved = new Dictionary<string, object>();

            foreach (var child in svgDocument
                                 .OfType<IGraphicalElement>()
                                 .Select(g => FromSvg(g, resolved)))
                doc.Root.Add(child, 0);

            return doc;
        }

        private static DG.ILayer FromSvg(
            IGraphicalElement element, IDictionary<string, object> resolved)
        {
            if (element.Id != null &&
                resolved.TryGetValue(element.Id, out var resolvedObject) &&
                resolvedObject is DG.ILayer resolvedLayer)
                return resolvedLayer;

            DG.ILayer layer = null;

            if (element is Group containerElement)
            {
                var group = new DG.Group();

                foreach (var child in containerElement
                                     .OfType<IGraphicalElement>()
                                     .Select(g => FromSvg(g, resolved)))
                    group.Add(child, 0);

                layer = group;
            }

            if (element is Use useElement)
            {
                var target = useElement.Document.Resolve<IGraphicalElement>(useElement.Target);
                if (target != null)
                    layer = new DG.Clone
                    {
                        Target = FromSvg(target, resolved)
                    };
            }
            else if (element is IShapeElement shapeElement)
            {
                DG.IGeometricLayer shape;

                // ReSharper disable RedundantNameQualifier
                switch (shapeElement)
                {
                    case Ellipse ellipse:
                        shape = new DG.Ellipse
                        {
                            CenterX = ellipse.CenterX.To(LengthUnit.Pixels),
                            CenterY = ellipse.CenterY.To(LengthUnit.Pixels),
                            RadiusX = ellipse.RadiusX.To(LengthUnit.Pixels),
                            RadiusY = ellipse.RadiusY.To(LengthUnit.Pixels)
                        };

                        break;
                    case Line line:
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
                    case Path path:
                        var pathPath = new DG.Path();

                        pathPath.Instructions.AddItems(path.Data);

                        shape = pathPath;

                        break;
                    case Polygon polygon:
                        var polygonPath = new DG.Path();

                        polygonPath.Instructions.AddItems(
                            polygon.Points.Select(v => new LinePathInstruction(v)));

                        polygonPath.Instructions.Add(new ClosePathInstruction(false));

                        shape = polygonPath;

                        break;
                    case Polyline polyline:
                        var polylinePath = new DG.Path();

                        polylinePath.Instructions.AddItems(
                            polyline.Points.Select(v => new LinePathInstruction(v)));

                        polylinePath.Instructions.Add(new ClosePathInstruction(true));

                        shape = polylinePath;

                        break;
                    case Rectangle rectangle:
                        shape = new DG.Rectangle
                        {
                            X = rectangle.X.To(LengthUnit.Pixels),
                            Y = rectangle.Y.To(LengthUnit.Pixels),
                            Width = rectangle.Width.To(LengthUnit.Pixels),
                            Height = rectangle.Height.To(LengthUnit.Pixels)
                        };

                        break;
                    case Circle circle:
                        shape = new DG.Ellipse
                        {
                            CenterX = circle.CenterX.To(LengthUnit.Pixels),
                            CenterY = circle.CenterY.To(LengthUnit.Pixels),
                            RadiusX = circle.Radius.To(LengthUnit.Pixels),
                            RadiusY = circle.Radius.To(LengthUnit.Pixels)
                        };

                        break;
                    case Text text:
                        shape = new DG.Text
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


                shape.Fill = FromSvg(shapeElement.Fill,
                                     shapeElement.FillOpacity,
                                     resolved,
                                     shapeElement.Document);
                shape.Stroke = FromSvg(shapeElement.Stroke,
                                       shapeElement.StrokeOpacity,
                                       shapeElement.StrokeWidth.To(LengthUnit.Pixels),
                                       shapeElement.StrokeDashArray,
                                       shapeElement.StrokeDashOffset,
                                       shapeElement.StrokeLineCap,
                                       shapeElement.StrokeLineJoin,
                                       shapeElement.StrokeMiterLimit,
                                       resolved,
                                       shapeElement.Document);

                layer = shape;
            }

            if (layer != null)
            {
                layer.ApplyTransform(element.Transform);

                layer.Name = element.Name ?? element.Id;

                if(layer.Name != null)
                resolved[layer.Name] = layer;
            }

            return layer;
        }

        private static IBrushInfo FromSvg(
            Paint.Paint paint, float opacity, IDictionary<string, object> resolved,
            Document svgDocument)
        {
            IBrushInfo brush;

            switch (paint)
            {
                case SolidColorPaint solidColor:
                    var color = solidColor.Color;

                    brush = new SolidColorBrushInfo(
                        new Color(color.Red, color.Green, color.Blue, color.Alpha * opacity));

                    break;

                case LinearGradientPaint linearGradient:

                    brush = new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(
                            linearGradient.Stops.Select(s => new GradientStop
                            {
                                Color = new Color(s.Color.Red,
                                                  s.Color.Green,
                                                  s.Color.Blue,
                                                  s.Color.Alpha * s.Opacity),
                                Offset = s.Offset.To(LengthUnit.Number, 1)
                            })),
                        StartPoint =
                           (linearGradient.X1, linearGradient.Y1).To(LengthUnit.Pixels, 1),
                        EndPoint =
                            (linearGradient.X2, linearGradient.Y2).To(LengthUnit.Pixels, 1),
                        Name = linearGradient.Name ?? linearGradient.Id,
                        Transform = linearGradient.Transform,
                        SpreadMethod = linearGradient.SpreadMethod,
                        Opacity = opacity,
                        Type = GradientBrushType.Linear
                    };

                    break;

                case RadialGradientPaint radialGradient:
                {
                    var stops = radialGradient.Stops.Select(s => new GradientStop
                    {
                        Color = new Color(s.Color.Red, s.Color.Green, s.Color.Blue, s.Color.Alpha),
                        Offset = s.Offset.To(LengthUnit.Number, 1)
                    });

                    var start =
                        (radialGradient.CenterX, radialGradient.CenterY).To(LengthUnit.Pixels, 1);
                    var focus =
                        (radialGradient.FocusX, radialGradient.FocusX).To(LengthUnit.Pixels, 1);
                    var radius =
                        (radialGradient.Radius, radialGradient.Radius).To(LengthUnit.Pixels, 1);

                    brush = new GradientBrushInfo
                    {
                        Stops = new ObservableList<GradientStop>(stops),
                        StartPoint = start,
                        FocusOffset = focus - start,
                        EndPoint = radius + start,
                        Name = radialGradient.Name ?? radialGradient.Id,
                        Transform = radialGradient.Transform,
                        SpreadMethod = radialGradient.SpreadMethod,
                        Opacity = opacity,
                        Type = GradientBrushType.Radial
                    };
                }

                    break;

                case ReferencePaint reference:

                    return FromSvg(svgDocument.ResolveDef<Paint.Paint>(reference.Reference),
                                   1,
                                   resolved,
                                   svgDocument);

                case null:

                    return null;

                default:

                    throw new NotImplementedException();
            }

            brush.Name = paint.Name;

            if (brush.Name != null)
                resolved.Add(brush.Name, brush);

            return brush;
        }

        private static IPenInfo FromSvg(
            Paint.Paint paint, float opacity, float width, IEnumerable<float> dashes,
            float dashOffset, LineCap lineCap, LineJoin lineJoin, float miterLimit,
            IDictionary<string, object> resolved, Document svgDocument)
        {
            var stroke = new PenInfo
            {
                Brush = FromSvg(paint, opacity, resolved, svgDocument),
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