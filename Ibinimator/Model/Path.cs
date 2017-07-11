using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public class Path : Shape
    {
        #region Properties

        public override String DefaultName => "Path";
        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            Parallel.ForEach(Nodes, node =>
            {
                if (node.X < x1) x1 = node.X;
                if (node.Y < y1) y1 = node.Y;
                if (node.X > x2) x2 = node.X;
                if (node.Y > y2) y2 = node.Y;
            });

            return new RectangleF(x1, y1, x2, y2);
        }

        public override Geometry GetGeometry(Factory factory)
        {
            PathGeometry pg = new PathGeometry(factory);
            GeometrySink gs = pg.Open();

            gs.BeginFigure(Nodes[0].Position, FigureBegin.Filled);

            for (int i = 1; i < Nodes.Count; i++)
            {
                switch (Nodes[i])
                {
                    case BezierNode bn:
                        var prevHandle = (Nodes[i - 1] as BezierNode)?.Control ?? Nodes[i - 1].Position;
                        gs.AddBezier(new BezierSegment()
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

            gs.EndFigure(FigureEnd.Closed);
            gs.Close();

            return pg;
        }

        #endregion Methods
    }

    public class BezierNode : PathNode
    {
        #region Properties

        public float ControlX { get => Get<float>(); set => Set(value); }
        public float ControlY { get => Get<float>(); set => Set(value); }
        public Vector2 Control => new Vector2(ControlX, ControlY);

        #endregion Properties
    }

    public class PathNode : Model
    {
        #region Properties

        public float X { get => Get<float>(); set => Set(value); }
        public float Y { get => Get<float>(); set => Set(value); }
        public Vector2 Position => new Vector2(X, Y);

        #endregion Properties
    }
}