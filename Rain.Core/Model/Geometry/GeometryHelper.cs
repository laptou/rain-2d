using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Geometry
{
    public static class GeometryHelper
    {
        public static IEnumerable<PathInstruction> InstructionsFromNodes(IEnumerable<PathNode> nodes)
        {
            var first = true;
            PathNode? previous = null;

            foreach (var node in nodes)
            {
                if (first)
                {
                    yield return new MovePathInstruction(node.Position);

                    previous = node;
                    first = false;

                    continue;
                }

                if (node.IncomingControl != null)
                    if (previous?.OutgoingControl != null)
                        yield return new CubicPathInstruction(node.Position,
                                                              previous.Value.OutgoingControl.Value,
                                                              node.IncomingControl.Value);
                    else
                        yield return new QuadraticPathInstruction(node.Position, node.IncomingControl.Value);
                else
                    yield return new LinePathInstruction(node.Position);

                if (node.FigureEnd != null)
                {
                    yield return new ClosePathInstruction(node.FigureEnd == PathFigureEnd.Open);

                    first = true;
                }

                previous = node;
            }
        }

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
                    switch (instruction)
                    {
                        case CubicPathInstruction cubic:
                            previousNode = new PathNode(previousNode.Index,
                                                        previousNode.Position,
                                                        previousNode.IncomingControl,
                                                        cubic.Control1,
                                                        previousNode.FigureEnd);

                            break;

                        case ClosePathInstruction close:
                            previousNode = new PathNode(previousNode.Index,
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
                        previousNode = new PathNode(index++, cubic.Position, cubic.Control2);

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

        public static IEnumerable<PathInstruction> ToInstructions(this IEnumerable<PathNode> nodes)
        {
            return InstructionsFromNodes(nodes);
        }

        public static IList<PathNode> ToNodeList(this IEnumerable<PathInstruction> instructions)
        {
            return NodesFromInstructions(instructions).ToList();
        }

        public static IEnumerable<PathNode> ToNodes(this IEnumerable<PathInstruction> instructions)
        {
            return NodesFromInstructions(instructions);
        }
    }
}