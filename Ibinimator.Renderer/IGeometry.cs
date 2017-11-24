using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Renderer
{
    public interface IGeometry : IResource
    {
        RectangleF Bounds();
        IGeometry Copy();
        IGeometry Difference(IGeometry other);
        bool FillContains(float x, float y);
        IGeometry Intersection(IGeometry other);
        void Load(IEnumerable<PathInstruction> source);
        IGeometrySink Open();
        IGeometry Outline(float width);
        IEnumerable<PathInstruction> Read();
        IEnumerable<PathNode> ReadNodes();
        void Read(IGeometrySink sink);
        bool StrokeContains(float x, float y, float width);
        IGeometry Transform(Matrix3x2 transform);
        IGeometry Union(IGeometry other);
        IGeometry Xor(IGeometry other);
    }

    public static class GeometryHelper
    {
        public static IEnumerable<PathNode> NodesFromInstructions(IEnumerable<PathInstruction> instructions)
        {
            // NOTE: this method WILL break if you pass anything other than cubics and line segments

            // we don't need to handle arcs below
            // because Pathify() converts everything
            // into line segments and cubic beziers

            PathNode previousNode = default;
            var index = 0;
            var start = true;

            foreach (var instruction in instructions)
            {
                if (!start)
                {
                    switch (instruction) {
                        case CubicPathInstruction cubic:
                            previousNode = new PathNode(
                                previousNode.Index,
                                previousNode.Position,
                                previousNode.IncomingControl,
                                cubic.Control1,
                                previousNode.FigureEnd);
                            break;

                        case ClosePathInstruction close:
                            previousNode = new PathNode(
                                previousNode.Index,
                                previousNode.Position,
                                previousNode.IncomingControl,
                                previousNode.OutgoingControl,
                                close.Open ? PathFigureEnd.Open : PathFigureEnd.Closed);
                            break;
                    }

                    yield return previousNode;
                }

                start = false;

                switch (instruction)
                {
                    case CubicPathInstruction cubic:
                        previousNode = new PathNode(
                            index++,
                            cubic.Position,
                            cubic.Control2);
                        break;

                    // lines and moves
                    case CoordinatePathInstruction line:
                        previousNode = new PathNode(index++, line.Position);
                        break;

                    case ClosePathInstruction _:
                        start = true;
                        break;

                    default: throw new Exception("wat");
                }
            }
        }

        public static IEnumerable<PathInstruction> InstructionsFromNodes(IEnumerable<PathNode> nodes)
        {
            var first = true;
            PathNode? previous = null;

            foreach (var node in nodes)
            {
                if (first)
                {
                    yield return new MovePathInstruction(node.Position);
                    first = false;
                }

                if (node.IncomingControl != null)
                {
                    if (previous?.OutgoingControl != null)
                    {
                        yield return new CubicPathInstruction(
                            node.Position,
                            previous.Value.OutgoingControl.Value,
                            node.IncomingControl.Value);
                    }
                    else
                    {
                        yield return new QuadraticPathInstruction(
                            node.Position,
                            node.IncomingControl.Value);
                    }
                }

                yield return new LinePathInstruction(node.Position);

                if (node.FigureEnd != null)
                {
                    yield return new ClosePathInstruction(node.FigureEnd == PathFigureEnd.Open);
                    first = true;
                }

                previous = node;
            }
        }
    }

    [DebuggerDisplay("{" + nameof(Position) + "}, {" + nameof(FigureEnd) + "}")]
    public struct PathNode
    {
        public Vector2 Position { get; }

        public Vector2? IncomingControl { get; }

        public Vector2? OutgoingControl { get; }

        public int Index { get; }

        public PathFigureEnd? FigureEnd { get; }

        public PathNode(int index, Vector2 position) : this()
        {
            Index = index;
            Position = position;
        }

        public PathNode(int index, Vector2 position, Vector2 incomingControl) : this(index, position)
        {
            IncomingControl = incomingControl;
        }

        public PathNode(int index, Vector2 position, Vector2 incomingControl, Vector2 outgoingControl) :
            this(index, position, incomingControl)
        {
            OutgoingControl = outgoingControl;
        }

        public PathNode(
            int index,
            Vector2 position,
            Vector2? incomingControl,
            Vector2? outgoingControl,
            PathFigureEnd? figureEnd) : this(
            index, position)
        {
            IncomingControl = incomingControl;
            OutgoingControl = outgoingControl;
            FigureEnd = figureEnd;
        }
    }

    public enum PathFigureEnd
    {
        Closed,
        Open
    }
}