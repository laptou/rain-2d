using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using WPF = System.Windows;
using System.Xml.Linq;
using Ibinimator.Svg;
using Ibinimator.View.Control;

namespace Ibinimator.View
{
    public class SvgExtension : MarkupExtension
    {
        public Uri Path { get; }

        public SvgExtension(Uri path)
        {
            Path = path;
        }

        private FrameworkElement ToWpf(IElement element)
        {
            if (element is IShapeElement shape)
            {
                WPF.Shapes.Shape wpfShape = null;

                switch (element)
                {
                    case Circle circle:
                        wpfShape = new WPF.Shapes.Ellipse
                        {
                            Width = circle.Radius.To(LengthUnit.Pixels) * 2,
                            Height = circle.Radius.To(LengthUnit.Pixels) * 2
                        };

                        wpfShape.SetValue(Canvas.LeftProperty, circle.CenterX - circle.Radius.To(LengthUnit.Pixels));
                        wpfShape.SetValue(Canvas.TopProperty, circle.CenterY - circle.Radius.To(LengthUnit.Pixels));
                        break;
                    case Ellipse ellipse:
                        wpfShape = new WPF.Shapes.Ellipse
                        {
                            Width = ellipse.RadiusX.To(LengthUnit.Pixels) * 2,
                            Height = ellipse.RadiusY.To(LengthUnit.Pixels) * 2
                        };

                        wpfShape.SetValue(Canvas.LeftProperty, ellipse.CenterX - ellipse.RadiusX.To(LengthUnit.Pixels));
                        wpfShape.SetValue(Canvas.TopProperty, ellipse.CenterY - ellipse.RadiusY.To(LengthUnit.Pixels));
                        break;
                    case Line line:
                        wpfShape = new WPF.Shapes.Line
                        {
                            X1 = line.X1,
                            Y1 = line.Y1,
                            X2 = line.X2,
                            Y2 = line.Y2
                        };
                        break;
                    case Path path:
                        var data = new PathGeometry();
                        var figure = new PathFigure();
                        var start = true;

                        foreach (var node in path.Data)
                        {
                            switch (node)
                            {
                                case ArcPathNode arcPathNode:
                                    figure.Segments.Add(
                                        new ArcSegment(
                                            new Point(node.X, node.Y),
                                            new Size(arcPathNode.RadiusX, arcPathNode.RadiusY), 
                                            arcPathNode.Rotation,
                                            arcPathNode.LargeArc,
                                            arcPathNode.Clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                                            true));
                                    break;
                                case CloseNode closeNode:
                                    figure.IsClosed = true;
                                    data.Figures.Add(figure);
                                    figure = new PathFigure();
                                    start = true;
                                    break;
                                case CubicPathNode cubicPathNode:
                                    figure.Segments.Add(
                                        new BezierSegment(
                                            new Point(cubicPathNode.Control1.X, cubicPathNode.Control1.Y),
                                            new Point(cubicPathNode.Control2.X, cubicPathNode.Control2.Y),
                                            new Point(node.X, node.Y),
                                            true));
                                    break;
                                case QuadraticPathNode quadraticPathNode:
                                    figure.Segments.Add(
                                        new QuadraticBezierSegment(
                                            new Point(quadraticPathNode.Control.X, quadraticPathNode.Control.Y),
                                            new Point(node.X, node.Y),
                                            true));
                                    break;
                                default:
                                    if (start)
                                    {
                                        figure.StartPoint = new Point(node.X, node.Y);
                                        start = false;
                                        continue;
                                    }

                                    figure.Segments.Add(new LineSegment(new Point(node.X, node.Y), true));
                                    break;
                            }
                        }

                        if (!start) data.Figures.Add(figure);

                        wpfShape = new WPF.Shapes.Path { Data = data };
                        break;
                    case Polygon polygon:
                        wpfShape = new WPF.Shapes.Polygon
                        {
                            Points = new PointCollection(polygon.Points.Select(v => new Point(v.X, v.Y)))
                        };
                        break;
                    case Polyline polyline:
                        wpfShape = new WPF.Shapes.Polyline
                        {
                            Points = new PointCollection(polyline.Points.Select(v => new Point(v.X, v.Y)))
                        };
                        break;
                    case Rectangle rectangle:
                        wpfShape = new WPF.Shapes.Rectangle
                        {
                            Width = rectangle.Width.To(LengthUnit.Pixels),
                            Height = rectangle.Height.To(LengthUnit.Pixels),
                            RadiusX = rectangle.RadiusX.To(LengthUnit.Pixels),
                            RadiusY = rectangle.RadiusY.To(LengthUnit.Pixels)
                        };

                        wpfShape.SetValue(Canvas.LeftProperty, rectangle.X);
                        wpfShape.SetValue(Canvas.TopProperty, rectangle.Y);
                        break;
                }

                wpfShape.Stroke = ToWpf(shape.Stroke.GetValueOrDefault());

                if (wpfShape.Stroke != null)
                {
                    wpfShape.Stroke.Opacity = shape.StrokeOpacity;
                    wpfShape.StrokeThickness = shape.StrokeWidth.To(LengthUnit.Pixels);
                    wpfShape.StrokeDashOffset = shape.StrokeDashOffset;
                    wpfShape.StrokeDashArray = new DoubleCollection(shape.StrokeDashArray.Cast<double>());
                }

                wpfShape.Fill = ToWpf(shape.Fill.GetValueOrDefault());

                if (wpfShape.Fill != null)
                    wpfShape.Fill.Opacity = shape.FillOpacity;

                return wpfShape;
            }

            if (element is IContainerElement container)
            {
                var canvas = new Canvas();

                foreach (var elem in container)
                    canvas.Children.Add(ToWpf(elem));

                return canvas;
            }

            return null;
        }

        private Brush ToWpf(Paint paint)
        {
            if (paint.Color != null)
            {
                var color = paint.Color.Value;

                var wpfColor = WPF.Media.Color.FromRgb(
                    (byte) (color.Red * 255),
                    (byte) (color.Green * 255),
                    (byte) (color.Blue * 255));

                wpfColor.A = (byte) (color.Alpha * 255);

                return new SolidColorBrush(wpfColor);
            }

            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var viewbox = new VisualBrush {Visual = new SvgImage {Source = Path}};

            return viewbox;
        }
    }
}
