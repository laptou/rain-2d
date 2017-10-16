using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Core;
using Vector2 = System.Numerics.Vector2;

namespace Ibinimator.Renderer.Model
{
    public class Path : Shape
    {
        public Path()
        {
            Nodes.CollectionChanged += OnNodesChanged;
        }

        private void OnNodesChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
                foreach (PathNode node in args.NewItems)
                    node.PropertyChanged += NodeOnPropertyChanged;

            if (args.Action == NotifyCollectionChangedAction.Remove)
                foreach (PathNode node in args.OldItems)
                    node.PropertyChanged -= NodeOnPropertyChanged;

            RaiseGeometryChanged();
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

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return cache.GetGeometry(this).Bounds();
        }

        public override IGeometry GetGeometry(ICacheManager cache)
        {
            var pg = cache.Context.RenderContext.CreateGeometry();
            var gs = pg.Open();

            if (Nodes.Count > 0)
            {
                // gs.SetFillMode(FillMode);

                var begin = true;

                foreach (var node in Nodes)
                {
                    if (begin)
                    {
                        gs.Move(node.X, node.Y);
                        begin = false;
                    }

                    switch (node)
                    {
                        case QuadraticPathNode cn:
                            gs.Quadratic(
                                cn.Position.X, cn.Position.Y,
                                cn.Control.X, cn.Control.Y);
                            break;

                        case CubicPathNode bn:
                            gs.Cubic(
                                bn.Position.X, bn.Position.Y,
                                bn.Control1.X, bn.Control1.Y,
                                bn.Control2.X, bn.Control2.Y);
                            break;

                        case ArcPathNode an:
                            gs.Arc(
                                an.Position.X, an.Position.Y,
                                an.RadiusX, an.RadiusY,
                                an.Rotation,
                                an.Clockwise,
                                an.LargeArc);
                            break;

                        case CloseNode close:
                            gs.Close(close.Open);
                            begin = true;
                            break;

                        case PathNode pn:
                            gs.Line(pn.Position.X, pn.Position.Y);
                            break;
                    }
                }
            }

            gs.Dispose();

            return pg;
        }

        public IGeometrySink Open()
        {
            return new MyGeometrySink(this);
        }

        private void NodeOnPropertyChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            RaiseGeometryChanged();
        }

        #region Nested type: MyGeometrySink

        private class MyGeometrySink : IGeometrySink
        {
            private readonly Path _path;
            private bool _closed = true;

            public MyGeometrySink(Path path)
            {
                _path = path;
            }

            public void Arc(float x, float y, float radiusX, float radiusY, float angle, bool clockwise, bool largeArc)
            {
                _closed = false;

                _path.Nodes.Add(new ArcPathNode
                {
                    Clockwise = clockwise,
                    LargeArc = largeArc,
                    RadiusX = radiusX,
                    RadiusY = radiusY,
                    Rotation = angle,
                    Position = new Vector2(x, y)
                });
            }

            public void Close(bool open)
            {
                if (_closed) return;

                _closed = true;
                _path.Nodes.Add(new CloseNode { Open = open });
            }

            public void Cubic(float x, float y, float cx1, float cy1, float cx2, float cy2)
            {
                _closed = false;
                _path.Nodes.Add(new CubicPathNode
                {
                    Control1 = new Vector2(cx1, cy1),
                    Control2 = new Vector2(cx2, cy2),
                    Position = new Vector2(x, y)
                });
            }

            public void Line(float x, float y)
            {
                _closed = false;
                _path.Nodes.Add(new PathNode
                {
                    Position = new Vector2(x, y)
                });
            }

            public void Move(float x, float y)
            {
                Close(true);
                Line(x, y);
            }

            public void Quadratic(float x, float y, float cx1, float cy1)
            {
                _closed = false;
                _path.Nodes.Add(new CubicPathNode
                {
                    Control1 = new Vector2(cx1, cy1),
                    Position = new Vector2(x, y)
                });
            }

            public void Dispose()
            {
            }

            public void Optimize()
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}