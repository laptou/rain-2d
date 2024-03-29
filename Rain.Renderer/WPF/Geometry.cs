﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Rain.Core.Model;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;

namespace Rain.Renderer.WPF
{
    using WPF = System.Windows.Media;

    internal class Geometry : ResourceBase, IGeometry, IOptimizedGeometry
    {
        public Geometry()
        {
            Path = new WPF.PathGeometry();
        }

        public Geometry(WPF.Geometry geometry)
        {
            Path = geometry.GetFlattenedPathGeometry(double.Epsilon, WPF.ToleranceType.Relative);
        }

        private IGeometry Combine(IGeometry other, WPF.GeometryCombineMode mode)
        {
            return new Geometry(new WPF.CombinedGeometry(mode, Path, ((Geometry) other).Path));
        }

        public static implicit operator WPF.Geometry(Geometry geometry) { return geometry.Path; }

        #region IGeometry Members

        public RectangleF Bounds() { return Path.Bounds.Convert(); }

        public IGeometry Copy() { return new Geometry(Path.Clone()); }

        public IGeometry Difference(IGeometry other) { return Combine(other, WPF.GeometryCombineMode.Exclude); }

        public override void Dispose() { Path = null; }

        /// <inheritdoc />
        public override bool Optimized => false;

        protected WPF.PathGeometry Path { get; set; }

        public bool FillContains(float x, float y)
        {
            return Path.FillContains(new Point(x, y), double.Epsilon, WPF.ToleranceType.Relative);
        }

        public IGeometry Intersection(IGeometry other) { return Combine(other, WPF.GeometryCombineMode.Intersect); }

        public void Load(IEnumerable<PathInstruction> source)
        {
            Path = new WPF.PathGeometry();

            var figure = new WPF.PathFigure();
            foreach (var instruction in source)
                switch (instruction)
                {
                    case ClosePathInstruction close:
                        figure.IsClosed = !close.Open;

                        if (figure.Segments.Count > 0)
                            Path.Figures.Add(figure);

                        figure = new WPF.PathFigure {IsFilled = true};

                        break;
                    case ArcPathInstruction arc:
                        figure.Segments.Add(new WPF.ArcSegment(arc.Position.Convert(),
                                                               new Size(arc.RadiusX, arc.RadiusY),
                                                               arc.Angle,
                                                               arc.LargeArc,
                                                               arc.Clockwise
                                                                   ? WPF.SweepDirection.Clockwise
                                                                   : WPF.SweepDirection.Counterclockwise,
                                                               true));

                        break;
                    case CubicPathInstruction cubic:
                        figure.Segments.Add(new WPF.BezierSegment(cubic.Control1.Convert(),
                                                                  cubic.Control2.Convert(),
                                                                  cubic.Position.Convert(),
                                                                  true));

                        break;
                    case LinePathInstruction line:
                        figure.Segments.Add(new WPF.LineSegment(line.Position.Convert(), true));

                        break;
                    case MovePathInstruction move:
                        if (figure.Segments.Count > 0)
                            Path.Figures.Add(figure);

                        figure = new WPF.PathFigure
                        {
                            StartPoint = move.Position.Convert(),
                            IsFilled = true
                        };

                        break;
                    case QuadraticPathInstruction quadratic:
                        figure.Segments.Add(
                            new WPF.QuadraticBezierSegment(quadratic.Control.Convert(),
                                                           quadratic.Position.Convert(),
                                                           true));

                        break;
                }

            if (figure.Segments.Count > 0)
                Path.Figures.Add(figure);
        }

        public IGeometrySink Open() { return new Sink(Path); }

        public IGeometry Outline(float width)
        {
            return new Geometry(Path.GetWidenedPathGeometry(new WPF.Pen(null, width)));
        }

        public IEnumerable<PathInstruction> Read()
        {
            foreach (var figure in Path.Figures)
            {
                yield return new MovePathInstruction((float) figure.StartPoint.X, (float) figure.StartPoint.Y);

                foreach (var segment in figure.Segments)
                    switch (segment)
                    {
                        case WPF.ArcSegment arc:

                            yield return new ArcPathInstruction((float) arc.Point.X,
                                                                (float) arc.Point.Y,
                                                                (float) arc.Size.Width,
                                                                (float) arc.Size.Height,
                                                                (float) arc.RotationAngle,
                                                                arc.SweepDirection == WPF.SweepDirection.Clockwise,
                                                                arc.IsLargeArc);

                            break;
                        case WPF.BezierSegment cubic:

                            yield return new CubicPathInstruction((float) cubic.Point3.X,
                                                                  (float) cubic.Point3.Y,
                                                                  (float) cubic.Point1.X,
                                                                  (float) cubic.Point1.Y,
                                                                  (float) cubic.Point2.X,
                                                                  (float) cubic.Point2.Y);

                            break;
                        case WPF.LineSegment line:

                            yield return new LinePathInstruction((float) line.Point.X, (float) line.Point.Y);

                            break;
                        case WPF.QuadraticBezierSegment quadratic:

                            yield return new QuadraticPathInstruction((float) quadratic.Point2.X,
                                                                      (float) quadratic.Point2.Y,
                                                                      (float) quadratic.Point1.X,
                                                                      (float) quadratic.Point1.Y);

                            break;
                    }

                yield return new ClosePathInstruction(!figure.IsClosed);
            }
        }

        public void Read(IGeometrySink sink) { throw new NotImplementedException(); }

        public IEnumerable<PathNode> ReadNodes()
        {
            // we don't need to handle arcs below
            // because Pathify() converts everything
            // into line segments and cubic beziers

            return GeometryHelper.NodesFromInstructions(Read());
        }

        public bool StrokeContains(float x, float y, float width)
        {
            return Path.StrokeContains(new WPF.Pen(null, width),
                                       new Point(x, y),
                                       double.Epsilon,
                                       WPF.ToleranceType.Relative);
        }

        public IGeometry Transform(Matrix3x2 transform) { throw new NotImplementedException(); }

        public IGeometry Union(IGeometry other) { return Combine(other, WPF.GeometryCombineMode.Union); }

        public IGeometry Xor(IGeometry other) { return Combine(other, WPF.GeometryCombineMode.Xor); }

        /// <inheritdoc />
        public virtual IOptimizedGeometry Optimize()
        {
            return this;

            // return new FreezeGeometry(Path);
        }

        /// <inheritdoc />
        public virtual IOptimizedGeometry Optimize(IPenInfo pen)
        {
            return this;

            // return new FreezeGeometry(Path);
        }

        #endregion

        #region Nested type: Sink

        public class Sink : IGeometrySink
        {
            private bool             _b;
            private WPF.PathGeometry _geometry;
            private float            _x;
            private float            _y;

            public Sink(WPF.PathGeometry geometry) { _geometry = geometry; }

            public void Optimize() { }

            private void Begin()
            {
                if (_b) return;

                _b = true;
                _geometry.Figures.Add(new WPF.PathFigure
                {
                    StartPoint = new Point(_x, _y),
                    IsFilled = true
                });
            }

            #region IGeometrySink Members

            public void Arc(float x, float y, float radiusX, float radiusY, float angle, bool clockwise, bool largeArc)
            {
                Begin();

                var last = _geometry.Figures.LastOrDefault();

                last?.Segments.Add(new WPF.ArcSegment
                {
                    Point = new Point(x, y),
                    Size = new Size(radiusX, radiusY),
                    RotationAngle = angle,
                    IsLargeArc = largeArc,
                    SweepDirection = clockwise ? WPF.SweepDirection.Clockwise : WPF.SweepDirection.Counterclockwise
                });

                (_x, _y) = (x, y);
            }

            public void Close(bool open)
            {
                var last = _geometry.Figures.LastOrDefault();

                if (last != null)
                    last.IsClosed = !open;

                _b = false;
            }

            public void Cubic(float x, float y, float cx1, float cy1, float cx2, float cy2)
            {
                Begin();

                var last = _geometry.Figures.LastOrDefault();

                last?.Segments.Add(new WPF.BezierSegment
                {
                    Point1 = new Point(cx1, cy1),
                    Point2 = new Point(cx2, cy2),
                    Point3 = new Point(x, y)
                });

                (_x, _y) = (x, y);
            }

            public void Dispose() { _geometry = null; }

            public void Line(float x, float y)
            {
                Begin();

                var last = _geometry.Figures.LastOrDefault();

                last?.Segments.Add(new WPF.LineSegment
                {
                    Point = new Point(x, y)
                });

                (_x, _y) = (x, y);
            }

            public void Move(float x, float y)
            {
                if (_b)
                    _geometry.Figures.Last()
                             .Segments.Add(new WPF.LineSegment
                              {
                                  Point = new Point(x, y)
                              });

                (_x, _y) = (x, y);
            }

            public void Quadratic(float x, float y, float cx1, float cy1)
            {
                Begin();

                var last = _geometry.Figures.LastOrDefault();

                last?.Segments.Add(new WPF.QuadraticBezierSegment
                {
                    Point1 = new Point(cx1, cy1),
                    Point2 = new Point(x, y)
                });

                (_x, _y) = (x, y);
            }

            #endregion
        }

        #endregion

        /// <inheritdoc />
        public GeometryOptimizationMode OptimizationMode =>
            GeometryOptimizationMode.Fill | GeometryOptimizationMode.Stroke;
    }

    internal sealed class FreezeGeometry : Geometry, IOptimizedGeometry
    {
        public FreezeGeometry(WPF.Geometry geometry) : base(geometry.GetAsFrozen() as WPF.Geometry)
        {
            OptimizationMode = GeometryOptimizationMode.Fill | GeometryOptimizationMode.Stroke;
        }

        /// <inheritdoc />
        public GeometryOptimizationMode OptimizationMode { get; }

        /// <inheritdoc />
        public override bool Optimized => true;

        /// <inheritdoc />
        public override IOptimizedGeometry Optimize() { return this; }

        /// <inheritdoc />
        public override IOptimizedGeometry Optimize(IPenInfo pen) { return this; }
    }
}