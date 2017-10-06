using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;

namespace Ibinimator.Renderer.Direct2D
{
    public class Geometry : IGeometry
    {
        private readonly D2D1.PathGeometry _geometry;
        private readonly D2D1.RenderTarget _target;

        public Geometry(D2D1.RenderTarget target)
        {
            _target = target;
            _geometry = new D2D1.PathGeometry(target.Factory);
        }

        public Geometry(D2D1.RenderTarget target, D2D1.Geometry source) : this(target)
        {
            using (var sink = new ReadingSink())
            {
                source.Simplify(D2D1.GeometrySimplificationOption.CubicsAndLines, sink);
                Load(this, sink.Read());
            }
        }

        private IGeometry Combine(IGeometry other, D2D1.CombineMode mode)
        {
            var geometry = new Geometry(_target);

            using (var sink = new ReadingSink())
            {
                _geometry.Combine(((Geometry) other)._geometry, mode, sink);
                Load(geometry, sink.Read());
            }

            return geometry;
        }

        private static void Load(IGeometry geometry, IEnumerable<PathInstruction> source)
        {
            using (var sink = geometry.Open())
            {
                foreach (var instruction in source)
                    switch (instruction)
                    {
                        case ClosePathInstruction close:
                            sink.Close(close.Open);
                            break;
                        case ArcPathInstruction arc:
                            sink.Arc(
                                arc.X, arc.Y,
                                arc.RadiusX, arc.RadiusY,
                                arc.Angle, arc.Clockwise, arc.LargeArc);
                            break;
                        case CubicPathInstruction cubic:
                            sink.Cubic(
                                cubic.X, cubic.Y,
                                cubic.Control1X, cubic.Control1Y,
                                cubic.Control2X, cubic.Control2Y);
                            break;
                        case LinePathInstruction line:
                            sink.Line(line.X, line.Y);
                            break;
                        case MovePathInstruction move:
                            sink.Move(move.X, move.Y);
                            break;
                        case QuadraticPathInstruction quadratic:
                            sink.Quadratic(
                                quadratic.X, quadratic.Y,
                                quadratic.ControlX, quadratic.ControlY);
                            break;
                    }
            }
        }

        private static IEnumerable<PathInstruction> Read(D2D1.PathGeometry geometry)
        {
            using (var sink = new ReadingSink())
            {
                geometry.Stream(sink);
                return sink.Read();
            }
        }

        public static implicit operator D2D1.Geometry(Geometry geometry)
        {
            return geometry._geometry;
        }

        #region IGeometry Members

        public IGeometry Copy()
        {
            var geometry = new Geometry(_target);

            Load(geometry, Read());

            return geometry;
        }

        public IGeometry Difference(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Exclude);
        }

        public void Dispose()
        {
            _geometry?.Dispose();
        }

        public bool FillContains(float x, float y)
        {
            return _geometry.FillContainsPoint(new RawVector2(x, y));
        }

        public IGeometry Intersection(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Intersect);
        }

        public IGeometrySink Open()
        {
            return new WritingSink(_geometry);
        }

        public void Optimize()
        {
            // maybe do a geometry realization, but that transforms this into a device-dependent
            // resource
        }

        public IGeometry Outline(float width)
        {
            var geometry = new Geometry(_target);

            using (var sink = new ReadingSink())
            {
                _geometry.Outline(sink);
                Load(geometry, sink.Read());
            }

            return geometry;
        }

        public IEnumerable<PathInstruction> Read()
        {
            return Read(_geometry);
        }

        public bool StrokeContains(float x, float y, float width)
        {
            return _geometry.StrokeContainsPoint(new RawVector2(x, y), width);
        }

        public IGeometry Union(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Union);
        }

        public IGeometry Xor(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Xor);
        }

        #endregion

        #region Nested type: ReadingSink

        private class ReadingSink : D2D1.GeometrySink
        {
            private readonly List<PathInstruction> _instructions = new List<PathInstruction>();

            public D2D1.FillMode FillMode { get; set; }

            public IEnumerable<PathInstruction> Read()
            {
                return _instructions;
            }

            #region GeometrySink Members

            public void AddArc(D2D1.ArcSegment arc)
            {
                _instructions.Add(
                    new ArcPathInstruction(
                        arc.Point.X, arc.Point.Y,
                        arc.Size.Width, arc.Size.Height,
                        arc.RotationAngle,
                        arc.SweepDirection == D2D1.SweepDirection.Clockwise,
                        arc.ArcSize == D2D1.ArcSize.Large));
            }

            public void AddBezier(D2D1.BezierSegment bezier)
            {
                _instructions.Add(
                    new CubicPathInstruction(
                        bezier.Point3.X, bezier.Point3.Y,
                        bezier.Point2.X, bezier.Point2.Y,
                        bezier.Point1.X, bezier.Point1.Y));
            }

            public void AddBeziers(D2D1.BezierSegment[] beziers)
            {
                foreach (var bezier in beziers)
                    AddBezier(bezier);
            }

            public void AddLine(RawVector2 point)
            {
                _instructions.Add(new LinePathInstruction(point.X, point.Y));
            }

            public void AddLines(RawVector2[] points)
            {
                foreach (var point in points)
                    AddLine(point);
            }

            public void AddQuadraticBezier(D2D1.QuadraticBezierSegment bezier)
            {
                _instructions.Add(
                    new QuadraticPathInstruction(
                        bezier.Point2.X, bezier.Point2.Y,
                        bezier.Point1.X, bezier.Point1.Y));
            }

            public void AddQuadraticBeziers(D2D1.QuadraticBezierSegment[] beziers)
            {
                foreach (var bezier in beziers)
                    AddQuadraticBezier(bezier);
            }

            public void BeginFigure(RawVector2 startPoint, D2D1.FigureBegin figureBegin)
            {
                _instructions.Add(new MovePathInstruction(startPoint.X, startPoint.Y));
            }

            public void Close()
            {
            }

            public void Dispose()
            {
                // do nothing; this is a managed class
                Shadow?.Dispose();
            }

            public void EndFigure(D2D1.FigureEnd figureEnd)
            {
                _instructions.Add(new ClosePathInstruction(figureEnd == D2D1.FigureEnd.Open));
            }

            public void SetFillMode(D2D1.FillMode fillMode)
            {
                FillMode = fillMode;
            }

            public void SetSegmentFlags(D2D1.PathSegment vertexFlags)
            {
                throw new NotImplementedException();
            }

            public IDisposable Shadow { get; set; }

            #endregion
        }

        #endregion

        #region Nested type: WritingSink

        private class WritingSink : IGeometrySink
        {
            private readonly D2D1.GeometrySink _sink;
            private bool _b;
            private float _x;
            private float _y;

            public WritingSink(D2D1.PathGeometry geometry)
            {
                _sink = geometry.Open();
            }

            private void Begin()
            {
                if (!_b)
                {
                    _b = true;
                    _sink.BeginFigure(new RawVector2(_x, _y), D2D1.FigureBegin.Filled);
                }
            }

            #region IGeometrySink Members

            public void Arc(float x, float y, float radiusX, float radiusY, float angle, bool clockwise, bool largeArc)
            {
                Begin();

                _sink.AddArc(new D2D1.ArcSegment
                {
                    Size = new Size2F(radiusX, radiusY),
                    ArcSize = largeArc ? D2D1.ArcSize.Large : D2D1.ArcSize.Small,
                    Point = new RawVector2(x, y),
                    RotationAngle = angle,
                    SweepDirection = clockwise ? D2D1.SweepDirection.Clockwise : D2D1.SweepDirection.CounterClockwise
                });

                (_x, _y) = (x, y);
            }

            public void Close(bool open)
            {
                Begin();
                _sink.EndFigure(open ? D2D1.FigureEnd.Open : D2D1.FigureEnd.Closed);
                _b = false;
            }

            public void Cubic(float x, float y, float cx1, float cy1, float cx2, float cy2)
            {
                Begin();
                _sink.AddBezier(new D2D1.BezierSegment
                {
                    Point1 = new RawVector2(cx1, cy1),
                    Point2 = new RawVector2(cx2, cy2),
                    Point3 = new RawVector2(x, y)
                });

                (_x, _y) = (x, y);
            }

            public void Dispose()
            {
                _sink.Close();
                _sink.Dispose();
            }

            public void Line(float x, float y)
            {
                Begin();
                _sink.AddLine(new RawVector2(x, y));

                (_x, _y) = (x, y);
            }

            public void Move(float x, float y)
            {
                if (_b)
                    _sink.AddLine(new RawVector2(x, y));

                (_x, _y) = (x, y);
            }

            public void Optimize()
            {
                // does nothing to this class
            }

            public void Quadratic(float x, float y, float cx1, float cy1)
            {
                Begin();
                _sink.AddQuadraticBezier(new D2D1.QuadraticBezierSegment
                {
                    Point1 = new RawVector2(cx1, cy1),
                    Point2 = new RawVector2(x, y)
                });
            }

            #endregion
        }

        #endregion
    }
}