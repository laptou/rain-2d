using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.Paint;

namespace Rain.Commands
{
    public sealed class ModifyGradientCommand : IOperationCommand<GradientBrushInfo>
    {
        public GradientStop Stop { get; }

        #region GradientOperation enum

        public enum GradientOperation
        {
            ChangeOffset,
            ChangeColor,
            ChangeFocus,
            ChangeEnd,
            ChangeStart,
            RemoveStop,
            AddStop
        }

        #endregion

        private ModifyGradientCommand(long id, GradientBrushInfo target)
        {
            Id = id;
            Target = target;
        }

        public ModifyGradientCommand(
            long id, float delta, IReadOnlyList<int> indices, GradientBrushInfo target) : this(
            id,
            target)
        {
            StopIndices = indices;
            ScalarDelta = delta;
            Operation = GradientOperation.ChangeOffset;
        }

        public ModifyGradientCommand(
            long id, Vector4 delta, IReadOnlyList<int> indices, GradientBrushInfo target) : this(
            id,
            target)
        {
            ColorDelta = delta;
            StopIndices = indices;
            Operation = GradientOperation.ChangeColor;
        }

        /// <summary>
        /// Creates a gradient modifier command with the <see cref="GradientOperation.ChangeStart"/>,
        /// <see cref="GradientOperation.ChangeFocus"/>, or <see cref="GradientOperation.ChangeEnd"/>
        /// operation, adjusting the property indicated by the operation.
        /// </summary>
        /// <param name="id">The id of the command.</param>
        /// <param name="delta">The amount to change the start position, focus, or end position by.</param>
        /// <param name="operation">The operation to apply.</param>
        /// <param name="target">The gradient to be modified.</param>
        public ModifyGradientCommand(
            long id, Vector2 delta, GradientOperation operation,
            GradientBrushInfo target) : this(id, target)
        {
            if (operation != GradientOperation.ChangeStart &&
                operation != GradientOperation.ChangeFocus &&
                operation != GradientOperation.ChangeEnd)
            {
                throw new ArgumentException(
                    $"{nameof(operation)} must be " + 
                    $"{nameof(GradientOperation.ChangeStart)}, " +
                    $"{nameof(GradientOperation.ChangeFocus)}, or " +
                    $"{nameof(GradientOperation.ChangeEnd)}.");
            }

            VectorDelta = delta;
            Operation = operation;
        }

        /// <summary>
        /// Creates a gradient modifier command with the <see cref="GradientOperation.RemoveStop"/>
        /// operation, removing the stops with the given indices.
        /// </summary>
        /// <param name="id">The id of the command.</param>
        /// <param name="indices">The indices of the stops to be removed.</param>
        /// <param name="target">The gradient to be modified.</param>
        public ModifyGradientCommand(
            long id, IReadOnlyList<int> indices,
            GradientBrushInfo target) : this(id, target)
        {
            StopIndices = indices;
            Operation = GradientOperation.RemoveStop;
        }

        /// <summary>
        /// Creates a gradient modifier command with the <see cref="GradientOperation.AddStop"/>
        /// operation, inserting a new gradient stop at the given index.
        /// </summary>
        /// <param name="id">The id of the command.</param>
        /// <param name="index">The index at which to insert the stop.</param>
        /// <param name="stop">The stop to insert.</param>
        /// <param name="target">The gradient to be modified.</param>
        public ModifyGradientCommand(
            long id, int index, GradientStop stop,
            GradientBrushInfo target) : this(id, target)
        {
            Stop = stop;
            StopIndices = new[] { index };
            Operation = GradientOperation.AddStop;
        }

        public Vector4 ColorDelta { get; }

        public GradientOperation Operation { get; }

        public float ScalarDelta { get; }

        public IReadOnlyList<int> StopIndices { get; }

        public GradientBrushInfo Target { get; }

        public Vector2 VectorDelta { get; }

        #region IOperationCommand<GradientBrushInfo> Members

        public void Do(IArtContext artContext)
        {
            switch (Operation)
            {
                case GradientOperation.ChangeOffset:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] = new GradientStop(
                            Target.Stops[stopIndex].Color,
                            Target.Stops[stopIndex].Offset + ScalarDelta);

                    break;
                case GradientOperation.ChangeColor:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] = new GradientStop(
                            Target.Stops[stopIndex].Color + ColorDelta,
                            Target.Stops[stopIndex].Offset);

                    break;
                case GradientOperation.ChangeFocus:
                    Target.Focus += VectorDelta;

                    break;
                case GradientOperation.ChangeEnd:
                    Target.EndPoint += VectorDelta;

                    break;
                case GradientOperation.ChangeStart:
                    Target.StartPoint += VectorDelta;

                    break;
                case GradientOperation.RemoveStop:
                    for (var i = 0; i < StopIndices.Count; i++)
                        Target.Stops.RemoveAt(StopIndices[i] - i);

                    break;
                case GradientOperation.AddStop:
                    Target.Stops.Insert(StopIndices[0], Stop);

                    break;
                default:

                    throw new ArgumentOutOfRangeException();
            }
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (newCommand is ModifyGradientCommand mgc &&
                mgc.Operation == Operation)
                switch (Operation)
                {
                    case GradientOperation.ChangeOffset:

                        return new ModifyGradientCommand(Id,
                                                         ScalarDelta + mgc.ScalarDelta,
                                                         StopIndices,
                                                         Target);
                    case GradientOperation.ChangeColor:

                        return new ModifyGradientCommand(Id,
                                                         ColorDelta + mgc.ColorDelta,
                                                         StopIndices,
                                                         Target);
                    case GradientOperation.ChangeFocus:
                    case GradientOperation.ChangeEnd:
                    case GradientOperation.ChangeStart:

                        return new ModifyGradientCommand(Id,
                                                         VectorDelta + mgc.VectorDelta,
                                                         Operation,
                                                         Target);
                    default:

                        return null;
                }

            return null;
        }

        public void Undo(IArtContext artContext)
        {
            switch (Operation)
            {
                case GradientOperation.ChangeOffset:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] = new GradientStop(
                            Target.Stops[stopIndex].Color,
                            Target.Stops[stopIndex].Offset - ScalarDelta);

                    break;
                case GradientOperation.ChangeColor:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] = new GradientStop(
                            Target.Stops[stopIndex].Color - ColorDelta,
                            Target.Stops[stopIndex].Offset);

                    break;
                case GradientOperation.ChangeFocus:
                    Target.Focus -= VectorDelta;

                    break;
                case GradientOperation.ChangeEnd:
                    Target.EndPoint -= VectorDelta;

                    break;
                case GradientOperation.ChangeStart:
                    Target.StartPoint -= VectorDelta;

                    break;
                case GradientOperation.RemoveStop:

                    throw new NotImplementedException();
                case GradientOperation.AddStop:

                    throw new NotImplementedException();
                default:

                    throw new ArgumentOutOfRangeException();
            }
        }

        public string Description => "Modified gradient";

        public long Id { get; }

        public long Time { get; } = Utility.Time.Now;

        GradientBrushInfo[] IOperationCommand<GradientBrushInfo>.Targets => new[] {Target};

        object[] IOperationCommand.Targets => new object[] {Target};

        #endregion
    }
}