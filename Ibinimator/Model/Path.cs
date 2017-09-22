using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ibinimator.Service;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Model
{
    [Serializable]
    [XmlType(nameof(Path))]
    public class Path : Shape
    {
        public Path()
        {
            Nodes.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                    foreach (PathNode node in args.NewItems)
                        node.PropertyChanged += NodeOnPropertyChanged;

                if (args.Action == NotifyCollectionChangedAction.Remove)
                    foreach (PathNode node in args.OldItems)
                        node.PropertyChanged -= NodeOnPropertyChanged;

                RaisePropertyChanged("Geometry");
            };
        }

        [XmlAttribute]
        public bool Closed
        {
            get => Nodes.LastOrDefault() is CloseNode;
            set
            {
                if (Closed && !value)
                {
                    Nodes.RemoveAt(Nodes.Count - 1);
                    RaisePropertyChanged(nameof(Closed));
                }

                if (!Closed && value)
                {
                    Nodes.Add(new CloseNode());
                    RaisePropertyChanged(nameof(Closed));
                }
            }
        }

        public override string DefaultName => "Path";

        public ObservableList<PathNode> Nodes { get; } = new ObservableList<PathNode>();

        protected override string ElementName => "path";

        public override RectangleF GetBounds(ICacheManager cache)
        {
            var first = Nodes.FirstOrDefault();
            float x1 = first?.X ?? 0,
                y1 = first?.Y ?? 0,
                x2 = first?.X ?? 0,
                y2 = first?.Y ?? 0;

            Parallel.ForEach(Nodes, node =>
            {
                if (node is CloseNode) return;
                if (node.X < x1) x1 = node.X;
                if (node.Y < y1) y1 = node.Y;
                if (node.X > x2) x2 = node.X;
                if (node.Y > y2) y2 = node.Y;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public override XElement GetElement()
        {
            var element = base.GetElement();

            if (Nodes.Count > 0)
            {
                var begin = true;
                var pathData = "";

                foreach (var pathNode in Nodes)
                {
                    if (begin)
                    {
                        pathData += $" M {Nodes.First().X},{Nodes.First().Y}";
                        begin = false;
                        continue;
                    }

                    switch (pathNode)
                    {
                        case CloseNode c:
                            pathData += $" Z";
                            begin = true;
                            break;
                        case QuadraticPathNode qn:
                            pathData += $" Q {qn.Control.X},{qn.Control.Y}" +
                                        $" {qn.X},{qn.Y}";
                            break;
                        case CubicPathNode cn:
                            pathData += $" C {cn.Control1.X},{cn.Control1.Y}" +
                                        $" {cn.Control2.X},{cn.Control2.Y}" +
                                        $" {cn.X},{cn.Y}";
                            break;
                        case PathNode pn:
                            pathData += $" L {pn.X},{pn.Y}";
                            break;
                    }
                }

                element.Add(new XAttribute("d", pathData));
            }

            return element;
        }

        public override Geometry GetGeometry(ICacheManager cache)
        {
            var pg = new PathGeometry(cache.ArtView.Direct2DFactory);
            var gs = pg.Open();

            if (Nodes.Count > 0)
            {
                gs.SetFillMode(FillMode);

                var begin = true;

                foreach (var node in Nodes)
                {
                    if (begin)
                    {
                        gs.BeginFigure(node.Position, FigureBegin.Filled);
                        begin = false;
                    }

                    switch (node)
                    {
                        case QuadraticPathNode cn:
                            gs.AddQuadraticBezier(new QuadraticBezierSegment
                            {
                                Point1 = cn.Control,
                                Point2 = cn.Position
                            });
                            break;

                        case CubicPathNode bn:
                            gs.AddBezier(new BezierSegment
                            {
                                Point1 = bn.Control1,
                                Point2 = bn.Control2,
                                Point3 = bn.Position
                            });
                            break;

                        case ArcPathNode an:
                            gs.AddArc(new ArcSegment
                            {
                                ArcSize = an.LargeArc ? ArcSize.Large : ArcSize.Small,
                                Point = an.Position,
                                Size = new Size2F(an.RadiusX, an.RadiusY),
                                RotationAngle = an.Rotation,
                                SweepDirection = an.Clockwise
                                    ? SweepDirection.Clockwise
                                    : SweepDirection.CounterClockwise
                            });
                            break;

                        case CloseNode close:
                            gs.EndFigure(close.Open ? FigureEnd.Open : FigureEnd.Closed);
                            begin = true;
                            break;

                        case PathNode pn:
                            gs.AddLine(pn.Position);
                            break;
                    }
                }

                if (!Closed)
                    gs.EndFigure(FigureEnd.Open);
            }

            gs.Close();
            gs.Dispose();

            return pg;
        }

        public GeometrySink Open()
        {
            return new MyGeometrySink(this);
        }

        private void NodeOnPropertyChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            RaisePropertyChanged("Geometry");
        }

        #region Nested type: MyGeometrySink

        private class MyGeometrySink : GeometrySink
        {
            private readonly Path _path;

            public MyGeometrySink(Path path)
            {
                _path = path;
            }

            #region GeometrySink Members

            public void AddArc(ArcSegment arc)
            {
                _path.Nodes.Add(new ArcPathNode
                {
                    Clockwise = arc.SweepDirection == SweepDirection.Clockwise,
                    LargeArc = arc.ArcSize == ArcSize.Large,
                    RadiusX = arc.Size.Width,
                    RadiusY = arc.Size.Height,
                    Rotation = arc.RotationAngle,
                    Position = arc.Point
                });
            }

            public void AddBezier(BezierSegment bezier)
            {
                _path.Nodes.Add(new CubicPathNode
                {
                    Control1 = bezier.Point1,
                    Control2 = bezier.Point2,
                    Position = bezier.Point3
                });
            }

            public void AddBeziers(BezierSegment[] beziers)
            {
                foreach (var bezier in beziers)
                    AddBezier(bezier);
            }

            public void AddLine(RawVector2 point)
            {
                _path.Nodes.Add(new PathNode {Position = point});
            }

            public void AddLines(RawVector2[] points)
            {
                foreach (var point in points)
                    AddLine(point);
            }

            public void AddQuadraticBezier(QuadraticBezierSegment bezier)
            {
                _path.Nodes.Add(new QuadraticPathNode
                {
                    Control = bezier.Point1,
                    Position = bezier.Point2
                });
            }

            public void AddQuadraticBeziers(QuadraticBezierSegment[] beziers)
            {
                foreach (var bezier in beziers)
                    AddQuadraticBezier(bezier);
            }

            public void BeginFigure(RawVector2 startPoint, FigureBegin figureBegin)
            {
                _path.Nodes.Add(new PathNode {Position = startPoint});
            }

            public void Close()
            {
            }

            public void Dispose()
            {
                // nothing to do here, no unmanaged resources
            }

            public void EndFigure(FigureEnd figureEnd)
            {
                _path.Nodes.Add(new CloseNode {Open = figureEnd == FigureEnd.Open});
            }

            public void SetFillMode(FillMode fillMode)
            {
                _path.FillMode = fillMode;
            }

            public void SetSegmentFlags(PathSegment vertexFlags)
            {
                throw new NotImplementedException();
            }

            public IDisposable Shadow { get; set; }

            #endregion
        }

        #endregion
    }

    [Serializable]
    public class CubicPathNode : PathNode
    {
        public Vector2 Control1
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public Vector2 Control2
        {
            get => Get<Vector2>();
            set => Set(value);
        }
    }

    [Serializable]
    public class QuadraticPathNode : PathNode
    {
        public Vector2 Control
        {
            get => Get<Vector2>();
            set => Set(value);
        }
    }

    public class CloseNode : PathNode
    {
        public bool Open
        {
            get => Get<bool>();
            set => Set(value);
        }
    }

    public class ArcPathNode : PathNode
    {
        public bool Clockwise
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LargeArc
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float RadiusX
        {
            get => Get<float>();
            set => Set(value);
        }

        public float RadiusY
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Rotation
        {
            get => Get<float>();
            set => Set(value);
        }
    }

    [Serializable]
    public class PathNode : Model
    {
        public Vector2 Position
        {
            get => new Vector2(X, Y);
            set => (X, Y) = (value.X, value.Y);
        }

        [XmlAttribute]
        public float X
        {
            get => Get<float>();
            set => Set(value);
        }

        [XmlAttribute]
        public float Y
        {
            get => Get<float>();
            set => Set(value);
        }
    }
}