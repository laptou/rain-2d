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
            get => Get<bool>();
            set => Set(value);
        }

        public override string DefaultName => "Path";

        protected override string ElementName => "path";

        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        public override RectangleF GetBounds()
        {
            var first = Nodes.FirstOrDefault();
            float x1 = first?.X ?? 0,
                y1 = first?.Y ?? 0,
                x2 = first?.X ?? 0,
                y2 = first?.Y ?? 0;

            Parallel.ForEach(Nodes, node =>
            {
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
                var pathData = $"M {Nodes.First().X},{Nodes.First().Y}";

                foreach (var pathNode in Nodes.Skip(1))
                    switch (pathNode)
                    {
                        case BezierNode bn:
                            pathData +=
                                $" C {bn.Control1.X},{bn.Control1.Y} {bn.Control2.X},{bn.Control2.Y} {bn.X},{bn.Y}";
                            break;
                        case PathNode pn:
                            pathData += $" L {pn.X},{pn.Y}";
                            break;
                    }

                if (Closed)
                    pathData += " Z";

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
                gs.SetFillMode(FillMode.Winding);

                gs.BeginFigure(Nodes[0].Position, FigureBegin.Filled);

                for (var i = 1; i < Nodes.Count; i++)
                    switch (Nodes[i])
                    {
                        case BezierNode bn:
                            // var prevHandle = (Nodes[i - 1] as BezierNode)?.Control ?? Nodes[i - 1].Position;
                            gs.AddBezier(new BezierSegment
                            {
                                Point1 = bn.Control1,
                                Point2 = bn.Control2,
                                Point3 = bn.Position
                            });
                            break;

                        case PathNode pn:
                            gs.AddLine(pn.Position);
                            break;
                    }

                gs.EndFigure(Closed ? FigureEnd.Closed : FigureEnd.Open);
            }

            gs.Close();

            return pg;
        }
    }

    [Serializable]
    public class BezierNode : PathNode
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