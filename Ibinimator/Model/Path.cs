using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    [Serializable]
    [XmlType(nameof(Path))]
    public class Path : Shape
    {
        public Path()
        {
            Nodes.CollectionChanged += (sender, args) => RaisePropertyChanged("Geometry");
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

        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        protected override string ElementName => "path";

        public override RectangleF GetBounds()
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

        public override Geometry GetGeometry(Factory factory)
        {
            var pg = new PathGeometry(factory);
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

                        case CloseNode _:
                            gs.EndFigure(FigureEnd.Closed);
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

            return pg;
        }
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
    }

    [Serializable]
    public class PathNode : Model
    {
        public Vector2 Position => new Vector2(X, Y);

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