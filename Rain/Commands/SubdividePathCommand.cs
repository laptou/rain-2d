using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Rain.Commands
{
    public sealed class SubdividePathCommand : LayerCommandBase<Path>
    {
        private Vector2[] _original;
        private bool _sFlag;

        public SubdividePathCommand(long id, Path target, int edge) : base(id, 
            new[] { target })
        {
            Edge = edge;
        }

        public int Edge { get; }

        public override string Description => "Subdivided path";

        public override void Do(IArtContext context)
        {
            var nodes = Enumerable.ToList(GeometryHelper.NodesFromInstructions(Targets[0].Instructions));

            var prev = nodes[Edge];
            var next = nodes[Edge + 1];

            PathNode current;

            if (next.IncomingControl == null &&
                prev.OutgoingControl == null)
            {
                var c = Vector2.Lerp(prev.Position, next.Position, 0.5f);

                _original = new Vector2[] { prev.Position, next.Position };

                current = new PathNode(Edge + 1, c);
            }
            else if (prev.OutgoingControl != null &&
                     next.IncomingControl != null)
            {
                var (p1, p2) = (prev.OutgoingControl.Value, next.IncomingControl.Value);
                var (c, s1, s2) = CurveAnalysis.DeCasteljau.Subdivide(0.5f,
                    prev.Position,
                    p1,
                    p2,
                    next.Position);

                _original = new Vector2[] { prev.Position, p1, p2, next.Position };

                prev = new PathNode(prev.Index, prev.Position, prev.IncomingControl, s1[1], prev.FigureEnd);
                current = new PathNode(Edge + 1, c, s1[2], s2[2]);
                next = new PathNode(next.Index + 1, next.Position, s2[1], next.OutgoingControl, next.FigureEnd);
            }
            else if ((next.IncomingControl != null &&
                      prev.OutgoingControl == null) ||
                     (next.IncomingControl == null &&
                      prev.OutgoingControl != null))
            {
                var p = (next.IncomingControl ?? prev.OutgoingControl).Value;

                var (c, s1, s2) = CurveAnalysis.DeCasteljau.Subdivide(0.5f,
                    prev.Position, p, next.Position);

                _sFlag = next.IncomingControl == null;

                _original = new Vector2[] { prev.Position, p, next.Position };

                s1 = CurveAnalysis.Cubic(s1[0], s1[1], s1[2]);
                s2 = CurveAnalysis.Cubic(s2[0], s2[1], s2[2]);

                prev = new PathNode(prev.Index, s1[0], prev.IncomingControl, s1[1], prev.FigureEnd);
                current = new PathNode(Edge + 1, c, s1[2], s2[1]);
                next = new PathNode(next.Index + 1, s2[3], s2[2], next.OutgoingControl, next.FigureEnd);

            }
            else
            {
                throw new Exception("This shouldn't even be possible.");
            }

            nodes[Edge] = prev;
            nodes[Edge + 1] = next;
            nodes.Insert(Edge + 1, current);

            Targets[0].Instructions.ReplaceRange(GeometryHelper.InstructionsFromNodes(nodes));
        }

        public override void Undo(IArtContext context)
        {
            var nodes = Targets[0].Instructions.ToNodeList();

            // remove the node that was added
            nodes.RemoveAt(Edge + 1);

            var prev = nodes[Edge];
            var next = nodes[Edge + 1];

            if(_original.Length == 4)
            {
                prev = new PathNode(prev.Index, 
                    _original[0], 
                    prev.IncomingControl, 
                    _original[1], 
                    prev.FigureEnd);

                next = new PathNode(next.Index - 1,
                    _original[3],
                    _original[2],
                    next.OutgoingControl,
                    next.FigureEnd);
            }
            else
            {
                if(_sFlag)
                {
                    prev = new PathNode(prev.Index,
                        _original[0],
                        prev.IncomingControl,
                        _original[1],
                        prev.FigureEnd);
                }
                else
                {
                    next = new PathNode(next.Index - 1,
                    _original[2],
                    _original[1],
                    next.OutgoingControl,
                    next.FigureEnd);
                }
            }

            nodes[Edge] = prev;
            nodes[Edge + 1] = next;

            Targets[0].Instructions.ReplaceRange(nodes.ToInstructions());
        }
    }
}
