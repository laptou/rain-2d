﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ibinimator.Renderer.WPF
{
    using WPF = System.Windows.Media;

    public class Geometry : IGeometry
    {
        private WPF.PathGeometry _geometry;

        public Geometry(WPF.Geometry geometry)
        {
            _geometry = geometry.GetFlattenedPathGeometry(double.Epsilon, WPF.ToleranceType.Relative);
        }

        private IGeometry Combine(IGeometry other, WPF.GeometryCombineMode mode)
        {
            return new Geometry(new WPF.CombinedGeometry(mode, _geometry, (other as Geometry)?._geometry));
        }

        #region IGeometry Members

        public IGeometry Copy()
        {
            return new Geometry(_geometry.Clone());
        }

        public IGeometry Difference(IGeometry other)
        {
            return Combine(other, WPF.GeometryCombineMode.Exclude);
        }

        public void Dispose()
        {
            _geometry = null;
        }

        public bool FillContains(float x, float y)
        {
            return _geometry.FillContains(
                new Point(x, y),
                double.Epsilon,
                WPF.ToleranceType.Relative);
        }

        public IGeometry Intersection(IGeometry other)
        {
            return Combine(other, WPF.GeometryCombineMode.Intersect);
        }

        public IGeometrySink Open()
        {
            return new Sink(_geometry);
        }

        public void Optimize()
        {
            _geometry.Freeze();
        }

        public IGeometry Outline(float width)
        {
            return new Geometry(_geometry.GetWidenedPathGeometry(new WPF.Pen(null, width)));
        }

        public IEnumerable<PathInstruction> Read()
        {
            foreach (var figure in _geometry.Figures)
            {
                yield return new MovePathInstruction(
                    (float) figure.StartPoint.X,
                    (float) figure.StartPoint.Y);

                foreach (var segment in figure.Segments)
                    switch (segment)
                    {
                        case WPF.ArcSegment arc:
                            yield return new ArcPathInstruction(
                                (float) arc.Point.X,
                                (float) arc.Point.Y,
                                (float) arc.Size.Width,
                                (float) arc.Size.Height,
                                (float) arc.RotationAngle,
                                arc.SweepDirection == WPF.SweepDirection.Clockwise,
                                arc.IsLargeArc);
                            break;
                        case WPF.BezierSegment cubic:
                            yield return new CubicPathInstruction(
                                (float) cubic.Point3.X,
                                (float) cubic.Point3.Y,
                                (float) cubic.Point1.X,
                                (float) cubic.Point1.Y,
                                (float) cubic.Point2.X,
                                (float) cubic.Point2.Y);
                            break;
                        case WPF.LineSegment line:
                            yield return new LinePathInstruction(
                                (float) line.Point.X,
                                (float) line.Point.Y);
                            break;
                        case WPF.QuadraticBezierSegment quadratic:
                            yield return new QuadraticPathInstruction(
                                (float) quadratic.Point2.X,
                                (float) quadratic.Point2.Y,
                                (float) quadratic.Point1.X,
                                (float) quadratic.Point1.Y);
                            break;
                    }

                yield return new ClosePathInstruction(figure.IsClosed);
            }
        }

        public bool StrokeContains(float x, float y, float width)
        {
            return _geometry.StrokeContains(
                new WPF.Pen(null, width),
                new Point(x, y),
                double.Epsilon,
                WPF.ToleranceType.Relative);
        }

        public IGeometry Union(IGeometry other)
        {
            return Combine(other, WPF.GeometryCombineMode.Union);
        }

        public IGeometry Xor(IGeometry other)
        {
            return Combine(other, WPF.GeometryCombineMode.Xor);
        }

        #endregion

        #region Nested type: Sink

        public class Sink : IGeometrySink
        {
            private bool _b;
            private WPF.PathGeometry _geometry;
            private float _x;
            private float _y;

            public Sink(WPF.PathGeometry geometry)
            {
                _geometry = geometry;
            }

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

            public void Dispose()
            {
                _geometry = null;
            }

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
                    _geometry.Figures.Last().Segments.Add(new WPF.LineSegment
                    {
                        Point = new Point(x, y)
                    });

                (_x, _y) = (x, y);
            }

            public void Optimize()
            {
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
    }
}