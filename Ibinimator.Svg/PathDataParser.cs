using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    internal static class PathDataParser
    {
        public static IEnumerable<PathNode> Parse(string data)
        {
            var nodes = new List<PathNode>();

            var commands = Regex.Matches(data ?? "",
                @"([MLHVCTSAZmlhvctsaz]){1}\s*(?:,?(\s*(?:[-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:[Ee][-+]?[0-9]+)?)\s*))*");
            var (start, pos, control, control2) = (Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);
            var lastInstruction = PathDataInstruction.Close;

            foreach (Match command in commands)
            {
                var parameters =
                    from set in command.Groups
                        .OfType<System.Text.RegularExpressions.Group>()
                        .Skip(2)
                    from Capture cap in set.Captures
                    select float.Parse(cap.Value);

                var coordinates = new Stack<float>(parameters.Reverse());
                var relative = char.IsLower(command.Groups[1].Value[0]);
                PathDataInstruction instruction;

                switch (char.ToUpper(command.Groups[1].Value[0]))
                {
                    case 'M':
                        instruction = PathDataInstruction.Move;
                        break;
                    case 'L':
                        instruction = PathDataInstruction.Line;
                        break;
                    case 'H':
                        instruction = PathDataInstruction.Horizontal;
                        break;
                    case 'V':
                        instruction = PathDataInstruction.Vertical;
                        break;
                    case 'C':
                        instruction = PathDataInstruction.Cubic;
                        break;
                    case 'S':
                        instruction = PathDataInstruction.ShortCubic;
                        break;
                    case 'Q':
                        instruction = PathDataInstruction.Quadratic;
                        break;
                    case 'T':
                        instruction = PathDataInstruction.ShortQuadratic;
                        break;
                    case 'A':
                        instruction = PathDataInstruction.Arc;
                        break;
                    case 'Z':
                        instruction = PathDataInstruction.Close;
                        break;
                    default:
                        throw new InvalidDataException("Invalid command.");
                }

                if (instruction != PathDataInstruction.Move &&
                    (lastInstruction == PathDataInstruction.Move ||
                     lastInstruction == PathDataInstruction.Close))
                    nodes.Add(new PathNode {X = pos.X, Y = pos.Y});

                switch (instruction)
                {
                    case PathDataInstruction.Move:
                        if (relative)
                            pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                        else
                            pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                        if (lastInstruction == PathDataInstruction.Close)
                        {
                            start = pos;
                            instruction = PathDataInstruction.Close;
                        }
                        else if (lastInstruction != PathDataInstruction.Move)
                        {
                            start = pos;
                            instruction = PathDataInstruction.Close;
                            nodes.Add(new CloseNode {Open = true});
                        }

                        if (coordinates.Count >= 2)
                        {
                            instruction = PathDataInstruction.Line;

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});

                            while (coordinates.Count >= 2)
                            {
                                if (relative)
                                    pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                                else
                                    pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                                nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                            }
                        }
                        break;

                    #region Linear

                    case PathDataInstruction.Line:
                        while (coordinates.Count >= 2)
                        {
                            if (relative)
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            else
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }

                        break;

                    case PathDataInstruction.Horizontal:
                        while (coordinates.Count >= 1)
                        {
                            if (relative)
                                pos.X += coordinates.Pop();
                            else
                                pos.X = coordinates.Pop();

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }
                        break;

                    case PathDataInstruction.Vertical:
                        while (coordinates.Count >= 1)
                        {
                            if (relative)
                                pos.Y += coordinates.Pop();
                            else
                                pos.Y = coordinates.Pop();

                            nodes.Add(new PathNode {X = pos.X, Y = pos.Y});
                        }
                        break;

                    #endregion

                    #region Quadratic

                    case PathDataInstruction.Quadratic:
                        while (coordinates.Count >= 4)
                        {
                            if (relative)
                            {
                                control = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new QuadraticPathNode
                            {
                                Control = control,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;
                    case PathDataInstruction.ShortQuadratic:
                        while (coordinates.Count >= 2)
                        {
                            if (lastInstruction == PathDataInstruction.Quadratic ||
                                lastInstruction == PathDataInstruction.ShortQuadratic)
                                control = pos - (control - pos);
                            else
                                control = pos;

                            if (relative)
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            else
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                            nodes.Add(new QuadraticPathNode
                            {
                                Control = control,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;

                    #endregion

                    #region Cubic

                    case PathDataInstruction.Cubic:
                        while (coordinates.Count >= 6)
                        {
                            if (relative)
                            {
                                control = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                control2 = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control = new Vector2(coordinates.Pop(), coordinates.Pop());
                                control2 = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new CubicPathNode
                            {
                                Control1 = control,
                                Control2 = control2,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;
                    case PathDataInstruction.ShortCubic:
                        while (coordinates.Count >= 4)
                        {
                            if (lastInstruction == PathDataInstruction.Cubic ||
                                lastInstruction == PathDataInstruction.ShortCubic)
                                control = pos - (control2 - pos);
                            else
                                control = pos;

                            if (relative)
                            {
                                control2 = pos + new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            }
                            else
                            {
                                control2 = new Vector2(coordinates.Pop(), coordinates.Pop());
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());
                            }

                            nodes.Add(new CubicPathNode
                            {
                                Control1 = control,
                                Control2 = control2,
                                X = pos.X,
                                Y = pos.Y
                            });
                        }
                        break;

                    #endregion

                    case PathDataInstruction.Arc:
                        while (coordinates.Count >= 7)
                        {
                            var node = new ArcPathNode
                            {
                                RadiusX = coordinates.Pop(),
                                RadiusY = coordinates.Pop(),
                                Rotation = coordinates.Pop(),
                                LargeArc = coordinates.Pop() == 1,
                                Clockwise = coordinates.Pop() == 1
                            };

                            if (relative)
                                pos += new Vector2(coordinates.Pop(), coordinates.Pop());
                            else
                                pos = new Vector2(coordinates.Pop(), coordinates.Pop());

                            node.X = pos.X;
                            node.Y = pos.Y;

                            nodes.Add(node);
                        }
                        break;

                    case PathDataInstruction.Close:
                        nodes.Add(new CloseNode());
                        pos = start;
                        break;
                }

                lastInstruction = instruction;
            }

            return nodes;
        }

        #region Nested type: PathDataInstruction

        private enum PathDataInstruction
        {
            Move,
            Line,
            Horizontal,
            Vertical,
            Cubic,
            ShortCubic,
            Quadratic,
            ShortQuadratic,
            Arc,
            Close
        }

        #endregion
    }
}