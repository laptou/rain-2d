using System;
using SharpDX;
using SharpDX.Direct2D1;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Shared;

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

        #region Properties

        public override string DefaultName => "Path";

        [XmlAttribute]
        public bool Closed
        {
            get => Get<bool>();
            set => Set(value);
        }

        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            var first = Nodes.FirstOrDefault();
            float x1 = first?.X ?? 0, y1 = first?.Y ?? 0, 
                x2 = first?.X ?? 0, y2 = first?.Y ?? 0;

            Parallel.ForEach(Nodes, node =>
            {
                if (node.X < x1) x1 = node.X;
                if (node.Y < y1) y1 = node.Y;
                if (node.X > x2) x2 = node.X;
                if (node.Y > y2) y2 = node.Y;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public override Geometry GetGeometry(Factory factory)
        {
            PathGeometry pg = new PathGeometry(factory);
            GeometrySink gs = pg.Open();

            if (Nodes.Count > 0)
            {
                gs.SetFillMode(FillMode.Winding);

                gs.BeginFigure(Nodes[0].Position, FigureBegin.Filled);

                for (int i = 1; i < Nodes.Count; i++)
                {
                    switch (Nodes[i])
                    {
                        case BezierNode bn:
                            var prevHandle = (Nodes[i - 1] as BezierNode)?.Control ?? Nodes[i - 1].Position;
                            gs.AddBezier(new BezierSegment
                            {
                                Point1 = prevHandle,
                                Point2 = bn.Control,
                                Point3 = bn.Position
                            });
                            break;

                        case PathNode pn:
                            gs.AddLine(pn.Position);
                            break;
                    }
                }

                gs.EndFigure(Closed ? FigureEnd.Closed : FigureEnd.Open);
            }

            gs.Close();

            return pg;
        }

        #endregion Methods
    }

    [Serializable]
    public class BezierNode : PathNode
    {
        #region Properties

        [XmlAttribute]
        public float ControlX { get => Get<float>(); set => Set(value); }

        [XmlAttribute]
        public float ControlY { get => Get<float>(); set => Set(value); }

        public Vector2 Control => new Vector2(ControlX, ControlY);

        #endregion Properties
    }

    [Serializable]
    public class PathNode : Model
    {
        #region Properties
        
        [XmlAttribute]
        public float X { get => Get<float>(); set => Set(value); }
        
        [XmlAttribute]
        public float Y { get => Get<float>(); set => Set(value); }

        public Vector2 Position => new Vector2(X, Y);

        #endregion Properties
    }
}