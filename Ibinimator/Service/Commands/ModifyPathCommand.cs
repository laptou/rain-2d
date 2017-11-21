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
            MoveHandle1,
            MoveHandle2
        }

        #endregion

        public ModifyPathCommand(long id, Path target, PathInstruction[] instructions,
            int index, NodeOperation operation) : base(id, new[] {target})
        {
            if (operation != NodeOperation.Add)
                throw new InvalidOperationException();

            Instructions = instructions;
            Indices = new[] {index};
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, PathInstruction[] instructions,
            int[] indices, NodeOperation operation) : base(id, new[] {target})
        {
            if (operation != NodeOperation.Add)
                throw new InvalidOperationException();

            Instructions = instructions;
            Indices = indices;
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, int[] indices,
            Vector2 delta, NodeOperation operation) : base(id, new[] {target})
        {
            if (operation != NodeOperation.Move &&
                operation != NodeOperation.MoveHandle1 &&
                operation != NodeOperation.MoveHandle2)
                throw new InvalidOperationException();

            Indices = indices;
            Delta = delta;
            Operation = operation;
        }

        public ModifyPathCommand(long id, Path target, int[] indices,
            NodeOperation operation) : base(id, new[] {target})
        {
            if (operation != NodeOperation.Remove)
                throw new InvalidOperationException();

            Indices = indices;
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
                    case NodeOperation.MoveHandle1:
                    case NodeOperation.MoveHandle2:
                        return "Modified node handle(s)";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public int[] Indices { get; }

        public PathInstruction[] Instructions { get; private set; }

        public NodeOperation Operation { get; }

        public override void Do(IArtContext artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    for (var i = 0; i < Indices.Length; i++)
                        target.Instructions.Insert(Indices[i], Instructions[i]);
                    break;
                case NodeOperation.Remove:
                    var j = 0;
                    Instructions = new PathInstruction[Indices.Length];

                    foreach (var i in Indices)
                    {
                        Instructions[j++] = target.Instructions[i];
                        target.Instructions.RemoveAt(i);
                    }

                    break;
                case NodeOperation.Move:
                    foreach (var i in Indices)
                        switch (target.Instructions[i])
                        {
                            case CubicPathInstruction cubic:
                                if (target.Instructions[i + 1] is CubicPathInstruction prevCubic)
                                    target.Instructions[i + 1] =
                                        new CubicPathInstruction(
                                            prevCubic.Position,
                                            prevCubic.Control1 + Delta,
                                            prevCubic.Control2
                                        );

                                target.Instructions[i] =
                                    new CubicPathInstruction(
                                        cubic.Position + Delta,
                                        cubic.Control1,
                                        cubic.Control2 + Delta
                                    );
                                break;
                            case QuadraticPathInstruction quad:
                                target.Instructions[i] =
                                    new QuadraticPathInstruction(
                                        quad.Position + Delta,
                                        quad.Control + Delta
                                    );
                                break;
                            case LinePathInstruction line:
                                target.Instructions[i] =
                                    new LinePathInstruction(line.Position + Delta);
                                break;
                            case MovePathInstruction move:
                                target.Instructions[i] =
                                    new MovePathInstruction(move.Position + Delta);
                                break;
                        }

                    break;
                case NodeOperation.MoveHandle1:
                    foreach (var i in Indices)
                        switch (target.Instructions[i])
                        {
                            case CubicPathInstruction cubic:
                                target.Instructions[i] =
                                    new CubicPathInstruction(
                                        cubic.Position,
                                        cubic.Control1 + Delta,
                                        cubic.Control2
                                    );
                                break;
                            case QuadraticPathInstruction quad:
                                target.Instructions[i] =
                                    new QuadraticPathInstruction(
                                        quad.Position,
                                        quad.Control + Delta
                                    );
                                break;
                        }
                    break;
                case NodeOperation.MoveHandle2:
                    foreach (var i in Indices)
                        if (target.Instructions[i] is CubicPathInstruction cubic)
                            target.Instructions[i] =
                                new CubicPathInstruction(
                                    cubic.Position,
                                    cubic.Control1,
                                    cubic.Control2 + Delta
                                );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Undo(IArtContext artView)
        {
            var target = Targets[0];

            switch (Operation)
            {
                case NodeOperation.Add:
                    target.Instructions.RemoveItems(Instructions);
                    break;
                case NodeOperation.Remove:
                    target.Instructions.SuspendCollectionChangeNotification();

                    for (var i = 0; i < Instructions.Length; i++)
                        target.Instructions.Insert(Indices[i], Instructions[i]);

                    target.Instructions.ResumeCollectionChangeNotification();
                    break;
                case NodeOperation.Move:
                    foreach (var i in Indices)
                        switch (target.Instructions[i])
                        {
                            case CubicPathInstruction cubic:
                                if (target.Instructions[i - 1] is CubicPathInstruction prevCubic)
                                    target.Instructions[i - 1] =
                                        new CubicPathInstruction(
                                            prevCubic.Position,
                                            prevCubic.Control1 - Delta,
                                            prevCubic.Control2
                                        );

                                target.Instructions[i] =
                                    new CubicPathInstruction(
                                        cubic.Position - Delta,
                                        cubic.Control1,
                                        cubic.Control2 - Delta
                                    );
                                break;
                            case QuadraticPathInstruction quad:
                                target.Instructions[i] =
                                    new QuadraticPathInstruction(
                                        quad.Position - Delta,
                                        quad.Control - Delta
                                    );
                                break;
                            case LinePathInstruction line:
                                target.Instructions[i] =
                                    new LinePathInstruction(line.Position - Delta);
                                break;
                            case MovePathInstruction move:
                                target.Instructions[i] =
                                    new MovePathInstruction(move.Position - Delta);
                                break;
                        }

                    break;
                case NodeOperation.MoveHandle1:
                    for (var i = 0; i < Indices.Length; i++)
                        switch (target.Instructions[Indices[i]])
                        {
                            case CubicPathInstruction cubic:
                                target.Instructions[Indices[i]] =
                                    new CubicPathInstruction(
                                        cubic.Position,
                                        cubic.Control1 - Delta,
                                        cubic.Control2
                                    );
                                break;
                            case QuadraticPathInstruction quad:
                                target.Instructions[Indices[i]] =
                                    new QuadraticPathInstruction(
                                        quad.Position,
                                        quad.Control - Delta
                                    );
                                break;
                        }

                    break;
                case NodeOperation.MoveHandle2:
                    foreach (var i in Indices)
                        if (target.Instructions[i] is CubicPathInstruction cubic)
                            target.Instructions[i] =
                                new CubicPathInstruction(
                                    cubic.Position,
                                    cubic.Control1,
                                    cubic.Control2 - Delta
                                );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}