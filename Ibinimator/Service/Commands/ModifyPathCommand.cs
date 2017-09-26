using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;

namespace Ibinimator.Service.Commands
{
    public sealed class ModifyPathCommand : LayerCommandBase<Path>
    {
        public ModifyPathCommand(long id, Path target, PathNode[] nodes, 
            int index, NodeOperation operation) : base(id, new[] { target })
        {
            if(operation != NodeOperation.Add)
                throw new InvalidOperationException();

            Nodes = nodes;
            Indices = new[] {index};
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, PathNode[] nodes, 
            int[] indices, NodeOperation operation) : base(id, new[] { target })
        {
            if (operation != NodeOperation.Remove)
                throw new InvalidOperationException();

            Nodes = nodes;
            Indices = indices;
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, PathNode[] nodes,
            Vector2 delta, NodeOperation operation) : base(id, new[] { target })
        {
            if (operation != NodeOperation.Move &&
                operation != NodeOperation.MoveHandle1 &&
                operation != NodeOperation.MoveHandle2)
                throw new InvalidOperationException();

            if(nodes.Length > 1 &&
               (operation == NodeOperation.MoveHandle1 ||
               operation == NodeOperation.MoveHandle2))
                throw new ArgumentException("Can only move one handle at a time.");

            Nodes = nodes;
            Delta = delta;
            Operation = operation;
        }

        public PathNode[] Nodes { get; }

        public int[] Indices { get; }

        public Vector2 Delta { get; }

        public NodeOperation Operation { get; }

        public override void Do(ArtView artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    target.Nodes.InsertItems(Nodes, Indices[0]);
                    break;
                case NodeOperation.Remove:
                    target.Nodes.RemoveItems(Nodes);
                    break;
                case NodeOperation.Move:
                    foreach (var node in Nodes)
                    {
                        if (node is CubicPathNode cubic)
                        {
                            cubic.Control1 += Delta;
                            cubic.Control2 += Delta;
                        }

                        if (node is QuadraticPathNode quadratic)
                            quadratic.Control += Delta;

                        node.Position += Delta;
                    }

                    break;
                case NodeOperation.MoveHandle1:
                {
                    // braces to avoid variable scope issues for
                    // cubic
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control1 += Delta;
                    else if (Nodes[0] is QuadraticPathNode quadratic)
                        quadratic.Control += Delta;
                }
                    break;
                case NodeOperation.MoveHandle2:
                {
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control2 += Delta;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Undo(ArtView artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    target.Nodes.RemoveItems(Nodes);
                    break;
                case NodeOperation.Remove:
                    target.Nodes.SuspendCollectionChangeNotification();

                    for (var i = 0; i < Nodes.Length; i++)
                        target.Nodes.Insert(Indices[i], Nodes[i]);

                    target.Nodes.ResumeCollectionChangeNotification();
                    break;
                case NodeOperation.Move:
                    foreach (var node in Nodes)
                    {
                        if (node is CubicPathNode cubic)
                        {
                            cubic.Control1 -= Delta;
                            cubic.Control2 -= Delta;
                        }

                        if (node is QuadraticPathNode quadratic)
                            quadratic.Control -= Delta;

                        node.Position -= Delta;
                    }
                    break;
                case NodeOperation.MoveHandle1:
                {
                    // braces to avoid variable scope issues for
                    // cubic
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control1 -= Delta;
                    else if (Nodes[0] is QuadraticPathNode quadratic)
                        quadratic.Control -= Delta;
                }
                    break;
                case NodeOperation.MoveHandle2:
                {
                    if (Nodes[0] is CubicPathNode cubic)
                        cubic.Control2 -= Delta;
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string Description
        {
            get
            {
                switch (Operation)
                {
                    case NodeOperation.Add:
                        return $"Added {Nodes.Length} node(s)";
                    case NodeOperation.Remove:
                        return $"Removed {Nodes.Length} node(s)";
                    case NodeOperation.Move:
                        return $"Moved {Nodes.Length} node(s)";
                    case NodeOperation.MoveHandle1:
                    case NodeOperation.MoveHandle2:
                        return "Modified node handle";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public enum NodeOperation { Add, Remove, Move, MoveHandle1, MoveHandle2 }
    }
}