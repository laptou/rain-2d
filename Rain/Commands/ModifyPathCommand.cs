using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

using System.Numerics;

namespace Rain.Commands
{
    public sealed class ModifyPathCommand : LayerCommandBase<Path>, IMergeableOperationCommand<Path>
    {
        #region NodeOperation enum

        public enum NodeOperation
        {
            Add,
            Remove,
            Move,
            MoveInHandle,
            MoveOutHandle,
            EndFigureClosed,
            EndFigureOpen
        }

        #endregion NodeOperation enum

        /// <summary>
        ///     This will create a command with <see cref="NodeOperation.Add" />
        ///     that inserts the nodes given
        ///     by <paramref name="nodes" />.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="nodes">The nodes to insert.</param>
        /// <param name="index">The index at which to insert them.</param>
        public ModifyPathCommand(
            long id, Path target, IEnumerable<PathNode> nodes, int index) :
            base(id, new[] { target })
        {
            Nodes = nodes.ToArray();
            Indices = new[] { index };
            Operation = NodeOperation.Add;
        }

        /// <summary>
        ///     This will create a command with <see cref="NodeOperation.Move" />,
        ///     <see cref="NodeOperation.MoveInHandle" />, or <see cref="NodeOperation.MoveOutHandle" />
        ///     that moves the handle for the nodes at the positions given by
        ///     <paramref name="indices" /> by <paramref name="delta" />.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="delta">The amount to move the handles.</param>
        /// <param name="indices">The indices of the nodes to modify.</param>
        /// <param name="operation">The operation to perform.</param>
        public ModifyPathCommand(
            long id, Path target, Vector2 delta, int[] indices, NodeOperation operation) : base(
            id,
            new[] { target })
        {
            if (operation != NodeOperation.Move &&
                operation != NodeOperation.MoveInHandle &&
                operation != NodeOperation.MoveOutHandle)
                throw new ArgumentException(nameof(operation));

            Indices = indices;
            Array.Sort(Indices);

            Delta = delta;
            Operation = operation;
        }

        /// <summary>
        ///     This will create a command with <see cref="NodeOperation.Remove" />,
        ///     <see cref="NodeOperation.EndFigureClosed" /> or x that
        ///     removes the nodes at indices given by <paramref name="indices" /> or closes
        ///     the path at those nodes.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="indices">The indices of the nodes to remove.</param>
        /// <param name="operation">The operation to apply.</param>
        public ModifyPathCommand(long id, Path target, int[] indices, NodeOperation operation) :
            base(id, new[] { target })
        {
            if (operation != NodeOperation.Remove &&
                operation != NodeOperation.EndFigureClosed)
                throw new ArgumentException(nameof(operation));

            Indices = indices;
            Array.Sort(Indices);

            Operation = operation;
        }

        public Vector2 Delta { get; }

        public override string Description
        {
            get
            {
                switch (Operation)
                {
                    case NodeOperation.Add:

                        return $"Added {Indices.Length} node(s)";

                    case NodeOperation.Remove:

                        return $"Removed {Indices.Length} node(s)";

                    case NodeOperation.Move:

                        return $"Moved {Indices.Length} node(s)";

                    case NodeOperation.MoveInHandle:
                    case NodeOperation.MoveOutHandle:

                        return "Modified node handle(s)";

                    default:

                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public int[] Indices { get; }

        public PathNode[] Nodes { get; private set; }

        public NodeOperation Operation { get; }

        public override void Do(IArtContext context)
        {
            Apply(context, Operation, Indices, Nodes, Delta);
        }

        public override void Undo(IArtContext context)
        {
            switch (Operation)
            {
                case NodeOperation.Add:
                    Apply(context, NodeOperation.Remove, Indices, Nodes, Delta);

                    break;

                case NodeOperation.Remove:
                    Apply(context, NodeOperation.Add, Indices, Nodes, Delta);

                    break;

                case NodeOperation.Move:
                    Apply(context, NodeOperation.Move, Indices, Nodes, -Delta);

                    break;

                case NodeOperation.MoveInHandle:
                    Apply(context, NodeOperation.MoveInHandle, Indices, Nodes, -Delta);

                    break;

                case NodeOperation.MoveOutHandle:
                    Apply(context, NodeOperation.MoveOutHandle, Indices, Nodes, -Delta);

                    break;

                case NodeOperation.EndFigureClosed:
                    Apply(context, NodeOperation.EndFigureOpen, Indices, Nodes, -Delta);

                    break;

                case NodeOperation.EndFigureOpen:
                    Apply(context, NodeOperation.EndFigureClosed, Indices, Nodes, -Delta);

                    break;

                default:

                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Apply(
            IArtContext context, NodeOperation operation, IReadOnlyList<int> indices,
            IReadOnlyList<PathNode> targetNodes, Vector2 delta)
        {
            var target = Targets[0];
            var geom = context.CacheManager.GetGeometry(target);
            var nodes = geom.ReadNodes().ToList();

            switch (operation)
            {
                case NodeOperation.Add:

                    for (var i = 0; i < Indices.Length; i++)
                    {
                        if (nodes.Count == indices[i] + i)
                            nodes[indices[i] + i - 1] = new PathNode(
                                nodes[indices[i] + i - 1].Index,
                                nodes[indices[i] + i - 1].Position,
                                nodes[indices[i] + i - 1].IncomingControl,
                                nodes[indices[i] + i - 1].OutgoingControl,
                                null);

                        nodes.Insert(indices[i] + i, targetNodes[i]);
                    }

                    break;

                case NodeOperation.Remove:
                    Nodes = new PathNode[indices.Count];

                    for (var i = 0; i < indices.Count; i++)
                    {
                        Nodes[i] = nodes[indices[i]];

                        var index = indices[i] - i;
                        var node = nodes[index];
                        nodes.RemoveAt(index);

                        // update the node before the one that was removed with
                        // the figure ending
                        nodes[index - 1] = new PathNode(nodes[index - 1].Index,
                                                        nodes[index - 1].Position + delta,
                                                        nodes[index - 1].IncomingControl + delta,
                                                        nodes[index - 1].OutgoingControl + delta,
                                                        node.FigureEnd);
                    }

                    break;

                case NodeOperation.Move:
                    foreach (var index in indices)
                        nodes[index] = new PathNode(nodes[index].Index,
                                                    nodes[index].Position + delta,
                                                    nodes[index].IncomingControl + delta,
                                                    nodes[index].OutgoingControl + delta,
                                                    nodes[index].FigureEnd);

                    break;

                case NodeOperation.MoveInHandle:
                    foreach (var index in indices)
                        nodes[index] = new PathNode(nodes[index].Index,
                                                    nodes[index].Position,
                                                    nodes[index].IncomingControl + delta,
                                                    nodes[index].OutgoingControl,
                                                    nodes[index].FigureEnd);

                    break;

                case NodeOperation.MoveOutHandle:
                    foreach (var index in indices)
                        nodes[index] = new PathNode(nodes[index].Index,
                                                    nodes[index].Position,
                                                    nodes[index].IncomingControl,
                                                    nodes[index].OutgoingControl + delta,
                                                    nodes[index].FigureEnd);

                    break;

                case NodeOperation.EndFigureClosed:
                    Nodes = new PathNode[indices.Count];

                    for (var i = 0; i < indices.Count; i++)
                    {
                        Nodes[i] = nodes[indices[i]];

                        var index = indices[i];

                        nodes[index] = new PathNode(nodes[index].Index,
                                                    nodes[index].Position,
                                                    nodes[index].IncomingControl,
                                                    nodes[index].OutgoingControl,
                                                    PathFigureEnd.Closed);
                    }

                    break;

                case NodeOperation.EndFigureOpen:
                    Nodes = new PathNode[indices.Count];

                    for (var i = 0; i < indices.Count; i++)
                    {
                        Nodes[i] = nodes[indices[i]];

                        var index = indices[i];

                        nodes[index] = new PathNode(nodes[index].Index,
                                                    nodes[index].Position,
                                                    nodes[index].IncomingControl,
                                                    nodes[index].OutgoingControl,
                                                    PathFigureEnd.Open);
                    }

                    break;

                default:

                    throw new ArgumentOutOfRangeException();
            }

            target.Instructions.ReplaceRange(nodes.ToInstructions());
        }

        public IOperationCommand<Path> Merge(IOperationCommand<Path> newCommand)
        {
            if (newCommand is ModifyPathCommand newMPC)
            {
                if (newMPC.Operation == Operation)
                {
                    switch (Operation)
                    {
                        case NodeOperation.Add:
                            if (Indices.Length != 1) break;
                            if (newMPC.Indices.Length != 1) break;
                            if (Indices[0] != newMPC.Indices[0]) break;

                            return new ModifyPathCommand(Id,
                                Targets[0],
                                Nodes.Concat(newMPC.Nodes),
                                Indices[0]);

                        case NodeOperation.Remove:
                            return new ModifyPathCommand(Id,
                                Targets[0],
                                Indices.Concat(newMPC.Indices).ToArray(),
                                NodeOperation.Remove);

                        case NodeOperation.Move:
                        case NodeOperation.MoveInHandle:
                        case NodeOperation.MoveOutHandle:
                            if (!Enumerable.SequenceEqual(Indices, newMPC.Indices))
                                break;

                            return new ModifyPathCommand(Id,
                                Targets[0],
                                Delta + newMPC.Delta,
                                Indices,
                                Operation);
                    }
                }
            }

            return null;
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            return Merge(newCommand as IOperationCommand<Path>);
        }
    }
}