using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Geometry;

using SharpDX;
using SharpDX.Mathematics.Interop;

using D2D1 = SharpDX.Direct2D1;
using Matrix3x2 = System.Numerics.Matrix3x2;
using RectangleF = Rain.Core.Model.RectangleF;
using Vector2 = System.Numerics.Vector2;

namespace Ibinimator.Renderer.Direct2D
{
    internal class Geometry : ResourceBase, IGeometry, IEquatable<Geometry>
    {
        private readonly D2D1.RenderTarget _target;
        private          D2D1.Geometry     _geom;

        public Geometry(D2D1.RenderTarget target) : this(target,
                                                         new D2D1.PathGeometry(target.Factory)) { }

        public Geometry(D2D1.RenderTarget target, D2D1.Geometry source)
        {
            _target = target;
            _geom = source;
        }

        public Geometry(D2D1.RenderTarget target, IEnumerable<IGeometry> geometries) : this(target)
        {
            var nativeGeometries = geometries.Select(g => ((Geometry) g)._geom).ToArray();
            _geom = new D2D1.GeometryGroup(_target.Factory,
                                           D2D1.FillMode.Winding,
                                           nativeGeometries);
        }

        private D2D1.PathGeometry Path
        {
            get
            {
                lock (_geom)
                {
                    if (_geom?.IsDisposed == true) return null;

                    return _geom?.QueryInterfaceOrNull<D2D1.PathGeometry>();
                }
            }
        }

        public override bool Equals(object obj) { return Equals(obj as Geometry); }

        public override int GetHashCode() { return (int) _geom.NativePointer; }

        private IGeometry Combine(IGeometry other, D2D1.CombineMode mode)
        {
            var geometry = new Geometry(_target);

            using (var sink = new ReadingSink())
            {
                _geom.Combine(((Geometry) other)._geom, mode, sink);
                geometry.Load(sink.Read());
            }

            return geometry;
        }

        private static void Load(IGeometrySink sink, IEnumerable<PathInstruction> source)
        {
            foreach (var instruction in source)
                switch (instruction)
                {
                    case ClosePathInstruction close:
                        sink.Close(close.Open);

                        break;
                    case ArcPathInstruction arc:
                        sink.Arc(arc.X,
                                 arc.Y,
                                 arc.RadiusX,
                                 arc.RadiusY,
                                 arc.Angle,
                                 arc.Clockwise,
                                 arc.LargeArc);

                        break;
                    case CubicPathInstruction cubic:
                        sink.Cubic(cubic.X,
                                   cubic.Y,
                                   cubic.Control1X,
                                   cubic.Control1Y,
                                   cubic.Control2X,
                                   cubic.Control2Y);

                        break;
                    case LinePathInstruction line:
                        sink.Line(line.X, line.Y);

                        break;
                    case MovePathInstruction move:
                        sink.Move(move.X, move.Y);

                        break;
                    case QuadraticPathInstruction quadratic:
                        sink.Quadratic(quadratic.X,
                                       quadratic.Y,
                                       quadratic.ControlX,
                                       quadratic.ControlY);

                        break;
                }
        }

        private void Pathify()
        {
            if (Path != null)
                return;

            if (_geom == null)
                return;

            using (var sink = new ReadingSink())
            {
                _geom.Simplify(D2D1.GeometrySimplificationOption.CubicsAndLines, 0.05f, sink);

                _geom = new D2D1.PathGeometry(_target.Factory);

                Load(sink.Read());
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

        public static bool operator ==(Geometry g1, Geometry g2) { return g1?._geom == g2?._geom; }

        public static implicit operator D2D1.Geometry(Geometry geometry) { return geometry._geom; }

        public static bool operator !=(Geometry geometry1, Geometry geometry2)
        {
            return !(geometry1 == geometry2);
        }

        #region IEquatable<Geometry> Members

        public bool Equals(Geometry other) { return other == this; }

        #endregion

        #region IGeometry Members

        public RectangleF Bounds()
        {
            var b = _geom.GetBounds();

            return new RectangleF(b.Left, b.Top, b.Right - b.Left, b.Bottom - b.Top);
        }

        public IGeometry Copy()
        {
            var geometry = new Geometry(_target);

            geometry.Load(Read());

            return geometry;
        }

        public IGeometry Difference(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Exclude);
        }

        public override void Dispose()
        {
            lock (this)
            {
                _geom?.Dispose();
            }

            base.Dispose();
        }

        public bool FillContains(float x, float y)
        {
            return _geom.FillContainsPoint(new RawVector2(x, y));
        }

        public IGeometry Intersection(IGeometry other)
        {
            return Combine(other, D2D1.CombineMode.Intersect);
        }

        public void Load(IEnumerable<PathInstruction> source)
        {
            using (var sink = Open())
            {
                Load(sink, source);
            }
        }

        public IGeometrySink Open()
        {
            _geom = new D2D1.PathGeometry(_target.Factory);

            return new WritingSink(Path);
        }

        public override void Optimize()
        {
            // maybe do a geometry realization, but that transforms this into a device-dependent
            // resource
        }

        public IGeometry Outline(float width)
        {
            var geometry = new Geometry(_target);

            using (var sink = new ReadingSink())
            {
                _geom.Outline(sink);
                geometry.Load(sink.Read());
            }

            return geometry;
        }

        public IEnumerable<PathInstruction> Read()
        {
            Pathify();

            return Path != null ? Read(Path) : Enumerable.Empty<PathInstruction>();
        }

        public void Read(IGeometrySink sink) { Load(sink, Read()); }

        public IEnumerable<PathNode> ReadNodes()
        {
            Pathify();

            // we don't need to handle arcs below
            // because Pathify() converts everything
            // into line segments and cubic beziers

            return GeometryHelper.NodesFromInstructions(Read());
        }

        public bool StrokeContains(float x, float y, float width)
        {
            return _geom.StrokeContainsPoint(new RawVector2(x, y), width);
        }

        public IGeometry Transform(Matrix3x2 transform)
        {
            return new Geometry(_target,
                                new D2D1.TransformedGeometry(
                                    _target.Factory,
                                    _geom,
                                    transform.Convert()));
        }

        public IGeometry Union(IGeometry other) { return Combine(other, D2D1.CombineMode.Union); }

        public IGeometry Xor(IGeometry other) { return Combine(other, D2D1.CombineMode.Xor); }

        #endregion

        #region Nested type: ReadingSink

        private class ReadingSink : D2D1.GeometrySink
        {
            private readonly List<PathInstruction> _instructions = new List<PathInstruction>();

            public D2D1.FillMode FillMode { get; set; }

            public IEnumerable<PathInstruction> Read() { return _instructions; }

            #region GeometrySink Members

            public void AddArc(D2D1.ArcSegment arc)
            {
                _instructions.Add(new ArcPathInstruction(arc.Point.X,
                                                         arc.Point.Y,
                                                         arc.Size.Width,
                                                         arc.Size.Height,
                                                         arc.RotationAngle,
                                                         arc.SweepDirection ==
                                                         D2D1.SweepDirection.Clockwise,
                                                         arc.ArcSize == D2D1.ArcSize.Large));
            }

            public void AddBezier(D2D1.BezierSegment bezier)
            {
                _instructions.Add(new CubicPathInstruction(bezier.Point3.X,
                                                           bezier.Point3.Y,
                                                           bezier.Point1.X,
                                                           bezier.Point1.Y,
                                                           bezier.Point2.X,
                                                           bezier.Point2.Y));
            }

            public void AddBeziers(D2D1.BezierSegment[] beziers)
            {
                foreach (var bezier in beziers)
                    AddBezier(bezier);
            }

            public void AddLine(RawVector2 point)
            {
                // eliminate zero-length lines
                if (_instructions.LastOrDefault() is CoordinatePathInstruction coord &&
                    Vector2.DistanceSquared(coord.Position, new Vector2(point.X, point.Y)) < 0.005f)
                    return;

                _instructions.Add(new LinePathInstruction(point.X, point.Y));
            }

            public void AddLines(RawVector2[] points)
            {
                foreach (var point in points)
                    AddLine(point);
            }

            public void AddQuadraticBezier(D2D1.QuadraticBezierSegment bezier)
            {
                _instructions.Add(new QuadraticPathInstruction(
                                      bezier.Point2.X,
                                      bezier.Point2.Y,
                                      bezier.Point1.X,
                                      bezier.Point1.Y));
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

            public void Close() { }

            public void Dispose()
            {
                // do nothing; this is a managed class
                Shadow?.Dispose();
            }

            public void EndFigure(D2D1.FigureEnd figureEnd)
            {
                _instructions.Add(new ClosePathInstruction(figureEnd == D2D1.FigureEnd.Open));
            }

            public void SetFillMode(D2D1.FillMode fillMode) { FillMode = fillMode; }

            public void SetSegmentFlags(D2D1.PathSegment vertexFlags)
            {
                // TODO: SetSegmentFlags
                // throw new NotImplementedException();
            }

            public IDisposable Shadow { get; set; }

            #endregion
        }

        #endregion

        #region Nested type: WritingSink

        private class WritingSink : IGeometrySink
        {
            private readonly D2D1.GeometrySink _sink;
            private          bool              _b;
            private          float             _x;
            private          float             _y;

            public WritingSink(D2D1.PathGeometry geometry) { _sink = geometry.Open(); }

            private void Begin()
            {
                if (!_b)
                {
                    _b = true;
                    _sink.BeginFigure(new RawVector2(_x, _y), D2D1.FigureBegin.Filled);
                }
            }

            #region IGeometrySink Members

            public void Arc(
                float x, float y, float radiusX, float radiusY, float angle, bool clockwise,
                bool largeArc)
            {
                Begin();

                _sink.AddArc(new D2D1.ArcSegment
                {
                    Size = new Size2F(radiusX, radiusY),
                    ArcSize = largeArc ? D2D1.ArcSize.Large : D2D1.ArcSize.Small,
                    Point = new RawVector2(x, y),
                    RotationAngle = angle,
                    SweepDirection =
                        clockwise
                            ? D2D1.SweepDirection.Clockwise
                            : D2D1.SweepDirection.CounterClockwise
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
                if (_b) Close(true);

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