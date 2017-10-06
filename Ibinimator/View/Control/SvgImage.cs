using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Ibinimator.Svg;
using WPF = System.Windows;
using static Ibinimator.Svg.LengthUnit;
using Color = System.Windows.Media.Color;
using Path = Ibinimator.Svg.Path;

// ReSharper disable PossibleInvalidOperationException

namespace Ibinimator.View.Control
{
    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for SvgImage.xaml
    /// </summary>
    public class SvgImage : WPF.FrameworkElement
    {
        private Document _document;
        private readonly Dictionary<IShapeElement, Brush> _fills = new Dictionary<IShapeElement, Brush>();
        private readonly Dictionary<IShapeElement, Pen> _strokes = new Dictionary<IShapeElement, Pen>();
        private readonly Dictionary<IShapeElement, Geometry> _geometries = new Dictionary<IShapeElement, Geometry>();

        public SvgImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        public Uri Source
        {
            get => (Uri) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        
        public static readonly WPF.DependencyProperty SourceProperty =
            WPF.DependencyProperty.Register("Source", typeof(Uri), typeof(SvgImage),
                new WPF.FrameworkPropertyMetadata(null,
                    WPF.FrameworkPropertyMetadataOptions.AffectsMeasure |
                    WPF.FrameworkPropertyMetadataOptions.AffectsRender, SourceChanged));

        private static void SourceChanged(WPF.DependencyObject d, WPF.DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgImage svgImage)
                svgImage.Update();
        }

        private bool _prepared;

        protected override WPF.Size MeasureOverride(WPF.Size availableSize)
        {
            if (_document == null)
                return WPF.Size.Empty;

            var width = availableSize.Width;
            var height = availableSize.Height;
            var aspect = width / height;

            var docWidth = _document.Viewbox.Width;
            var docHeight = _document.Viewbox.Height;
            var docAspect = docWidth / docHeight;

            return aspect > docAspect
                ? new WPF.Size(docWidth * height / docHeight, height)
                : new WPF.Size(width, docHeight * width / docWidth);
        }

        protected override WPF.Size ArrangeOverride(WPF.Size finalSize)
        {
            return MeasureOverride(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var docDim = Math.Max(_document.Viewbox.Width, _document.Viewbox.Height);
            var viewDim = Math.Max(RenderSize.Width, RenderSize.Height);
            var scale = viewDim / docDim;

            drawingContext.PushTransform(new ScaleTransform(scale, scale));

            if(RenderSize.Width > RenderSize.Height)
                drawingContext.PushTransform(new TranslateTransform((RenderSize.Width - _document.Viewbox.Width * scale) / 2, 0));
            else
                drawingContext.PushTransform(new TranslateTransform(0, (RenderSize.Height - _document.Viewbox.Height * scale) / 2));


            if (_prepared)
                Render(_document, drawingContext);
        }

        private void Render(IGraphicalElement element, DrawingContext drawingContext)
        {
            drawingContext.PushTransform(
                new MatrixTransform(
                    element.Transform.M11,
                    element.Transform.M12,
                    element.Transform.M21,
                    element.Transform.M22,
                    element.Transform.M31,
                    element.Transform.M32
                ));

            if (element is IContainerElement container)
                foreach (var child in container.OfType<IGraphicalElement>())
                    Render(child, drawingContext);

            if (element is IShapeElement shape)
            {
                Brush fill = null;
                Pen stroke = null;

                if (shape.Fill != null)
                    fill = _fills[shape];

                if (shape.Stroke != null)
                    stroke = _strokes[shape];

                drawingContext.DrawGeometry(fill, stroke, _geometries[shape]);
            }

            drawingContext.Pop(); // remove transform
        }

        private void Update()
        {
            if (Source == null) return;

            _prepared = false;

            XDocument xdoc;
            

            using (var stream = GetStream())
                xdoc = XDocument.Load(stream);

            _document = new Document();
            _document.FromXml(xdoc.Root, new SvgContext());

            _fills.Clear();
            _strokes.Clear();
            _geometries.Clear();

            Prepare(_document);

            _prepared = true;
        }

        private Stream GetStream()
        {
            try
            {
                return WPF.Application.GetResourceStream(Source)?.Stream;
            }
            catch
            {
                return File.OpenRead(Source.AbsolutePath);
            }
        }

        private void Prepare(IElement element)
        {
            if (element is IContainerElement container)
                foreach (var child in container)
                    Prepare(child);

            if (element is IShapeElement shape)
            {
                var fill = ToWpf(shape.Fill, shape.FillOpacity);
                fill.Freeze();
                _fills[shape] = fill;

                var pen = new Pen(
                    ToWpf(shape.Stroke, shape.StrokeOpacity),
                    shape.StrokeWidth.To(Pixels));
                pen.Freeze();
                _strokes[shape] = pen;

                Geometry wpfShape;

                switch (element)
                {
                    case Circle circle:
                        wpfShape = new EllipseGeometry
                        {
                            RadiusX = circle.Radius.To(Pixels),
                            RadiusY = circle.Radius.To(Pixels),
                            Center = new WPF.Point(circle.CenterX.To(Pixels), circle.CenterY.To(Pixels))
                        };
                        break;
                    case Ellipse ellipse:
                        wpfShape = new EllipseGeometry
                        {
                            RadiusX = ellipse.RadiusX.To(Pixels),
                            RadiusY = ellipse.RadiusY.To(Pixels),
                            Center = new WPF.Point(ellipse.CenterX.To(Pixels), ellipse.CenterY.To(Pixels))
                        };
                        break;
                    case Line line:
                        wpfShape = new LineGeometry
                        {
                            StartPoint = new WPF.Point(line.X1.To(Pixels), line.Y1.To(Pixels)),
                            EndPoint = new WPF.Point(line.X2.To(Pixels), line.Y2.To(Pixels))
                        };
                        break;
                    case Path path:
                        var data = new PathGeometry();
                        var figure = new PathFigure();
                        var start = true;

                        foreach (var node in path.Data)
                            switch (node)
                            {
                                case ArcPathNode arcPathNode:
                                    figure.Segments.Add(
                                        new ArcSegment(
                                            new WPF.Point(node.X, node.Y),
                                            new WPF.Size(arcPathNode.RadiusX, arcPathNode.RadiusY),
                                            arcPathNode.Rotation,
                                            arcPathNode.LargeArc,
                                            arcPathNode.Clockwise
                                                ? SweepDirection.Clockwise
                                                : SweepDirection.Counterclockwise,
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
                                            new WPF.Point(cubicPathNode.Control1.X, cubicPathNode.Control1.Y),
                                            new WPF.Point(cubicPathNode.Control2.X, cubicPathNode.Control2.Y),
                                            new WPF.Point(node.X, node.Y),
                                            true));
                                    break;
                                case QuadraticPathNode quadraticPathNode:
                                    figure.Segments.Add(
                                        new QuadraticBezierSegment(
                                            new WPF.Point(quadraticPathNode.Control.X, quadraticPathNode.Control.Y),
                                            new WPF.Point(node.X, node.Y),
                                            true));
                                    break;
                                default:
                                    if (start)
                                    {
                                        figure.StartPoint = new WPF.Point(node.X, node.Y);
                                        start = false;
                                        continue;
                                    }

                                    figure.Segments.Add(new LineSegment(new WPF.Point(node.X, node.Y), true));
                                    break;
                            }

                        if (!start) data.Figures.Add(figure);

                        wpfShape = data;
                        break;
                    case Polygon polygon:
                        var polygonGeom = new StreamGeometry();
                        using (var polygonGeomCtx = polygonGeom.Open())
                        {
                            var points = polygon.Points.Select(v => new WPF.Point(v.X, v.Y)).ToList();

                            if (points.Count > 0)
                            {
                                polygonGeomCtx.BeginFigure(points[0], true, true);
                                polygonGeomCtx.PolyLineTo(points, true, true);
                            }
                        }
                        wpfShape = polygonGeom;
                        break;
                    case Polyline polyline:
                        var polylineGeom = new StreamGeometry();
                        using (var polylineGeomCtx = polylineGeom.Open())
                        {
                            var points = polyline.Points.Select(v => new WPF.Point(v.X, v.Y)).ToList();

                            if (points.Count > 0)
                            {
                                polylineGeomCtx.BeginFigure(points[0], true, false);
                                polylineGeomCtx.PolyLineTo(points, true, true);
                            }
                        }
                        wpfShape = polylineGeom;
                        break;
                    case Rectangle rectangle:
                        wpfShape = new RectangleGeometry
                        {
                            Rect = new WPF.Rect
                            {
                                X = rectangle.X.To(Pixels),
                                Y = rectangle.Y.To(Pixels),
                                Width = rectangle.Width.To(Pixels),
                                Height = rectangle.Height.To(Pixels)
                            },
                            RadiusX = rectangle.RadiusX.To(Pixels),
                            RadiusY = rectangle.RadiusY.To(Pixels)
                        };
                        break;
                    default:
                        throw new InvalidDataException();
                }

                wpfShape.Freeze();

                _geometries[shape] = wpfShape;
            }
        }

        private Brush ToWpf(Paint paint, float opacity)
        {
            if (paint is SolidColor solidColor)
            {
                var color = solidColor.Color;

                var wpfColor = Color.FromArgb(
                    (byte) (color.Alpha * opacity * 255),
                    (byte) (color.Red * 255),
                    (byte) (color.Green * 255),
                    (byte) (color.Blue * 255));

                return new SolidColorBrush(wpfColor);
            }

            return null;
        }
    }
}