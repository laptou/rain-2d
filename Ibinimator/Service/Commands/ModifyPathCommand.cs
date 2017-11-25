using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class ModifyPathCommand : LayerCommandBase<Path>
    {
        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is cannot be merged.");
        }

        #region NodeOperation enum

        public enum NodeOperation
        {
            Add,
            Remove,
            Move,
            MoveInHandle,
            MoveOutHandle
        }

        #endregion

        /// <summary>
        /// This will create a command with <see cref="NodeOperation.Add"/> that
        /// inserts the nodes given by <paramref name="nodes"/>.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="nodes">The nodes to insert.</param>
        /// <param name="index">The index at which to insert them.</param>
        public ModifyPathCommand(
            long id, Path target, IEnumerable<PathNode> nodes, int index) :
            base(id, new[] {target})
        {
            Nodes = nodes.ToArray();
            Indices = new[] {index};
            Operation = NodeOperation.Add;
        }

        /// <summary>
        /// This will create a command with <see cref="NodeOperation.Move"/>, 
        /// <see cref="NodeOperation.MoveInHandle"/>, or <see cref="NodeOperation.MoveOutHandle"/>
        /// that moves the handle for the nodes at the positions given by
        /// <paramref name="indices"/> by <paramref name="delta"/>.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="delta">The amount to move the handles.</param>
        /// <param name="indices">The indices of the nodes to modify.</param>
        /// <param name="operation">The operation to perform.</param>
        public ModifyPathCommand(
            long id, Path target, Vector2 delta, int[] indices, NodeOperation operation) :
            base(id, new[] {target})
        {
            if (operation != NodeOperation.Move &&
                operation != NodeOperation.MoveInHandle &&
                operation != NodeOperation.MoveOutHandle)
                throw new InvalidOperationException();

            Indices = indices;
            Array.Sort(Indices);

            Delta = delta;
            Operation = operation;
        }

        /// <summary>
        /// This will create a command with <see cref="NodeOperation.Remove"/> that
        /// remove the nodes at indices given by <paramref name="indices"/>.
        /// </summary>
        /// <param name="id">The command ID.</param>
        /// <param name="target">The path to modify.</param>
        /// <param name="indices">The indices of the nodes to remove.</param>
        public ModifyPathCommand(long id, Path target, int[] indices) :
            base(id, new[] {target})
        {
            Indices = indices;
            Array.Sort(Indices);

            Operation = NodeOperation.Remove;
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
                        nodes.Insert(indices[i] + i, targetNodes[i]);
                    break;
                case NodeOperation.Remove:
                    Nodes = new PathNode[indices.Count];

                    for (var i = 0; i < indices.Count; i++)
                    {
                        Nodes[i] = nodes[indices[i]];
                        nodes.RemoveAt(indices[i] - i);
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            target.Instructions.Clear();
            target.Instructions.AddItems(GeometryHelper.InstructionsFromNodes(nodes));
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}