﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class ModifyGradientCommand : IOperationCommand<GradientBrushInfo>
    {
        private ModifyGradientCommand(long id, GradientBrushInfo target)
        {
            Id = id;
            Target = target;
        }

        public ModifyGradientCommand(long id, float delta, IReadOnlyList<int> indices, GradientBrushInfo target)
            : this(id, target)
        {
            StopIndices = indices;
            ScalarDelta = delta;
            Operation = GradientOperation.ChangeOffset;
        }

        public ModifyGradientCommand(long id, Vector4 delta, IReadOnlyList<int> indices, GradientBrushInfo target)
            : this(id, target)
        {
            ColorDelta = delta;
            StopIndices = indices;
            Operation = GradientOperation.ChangeColor;
        }

        public ModifyGradientCommand(
            long id, Vector2 delta, IReadOnlyList<int> indices,
            GradientOperation operation, GradientBrushInfo target)
            : this(id, target)
        {
            VectorDelta = delta;
            StopIndices = indices;
            Operation = operation;
        }

        public GradientBrushInfo Target { get; }

        public float ScalarDelta { get; }

        public Vector2 VectorDelta { get; }

        public Vector4 ColorDelta { get; }

        public GradientOperation Operation { get; }

        public IReadOnlyList<int> StopIndices { get; }

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


        #region IOperationCommand<GradientBrushInfo> Members

        public void Do(IArtContext artContext)
        {
            switch (Operation)
            {
                case GradientOperation.ChangeOffset:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] =
                            new GradientStop(Target.Stops[stopIndex].Color,
                                             Target.Stops[stopIndex].Offset + ScalarDelta);
                    break;
                case GradientOperation.ChangeColor:
                    foreach (var stopIndex in StopIndices)
                        Target.Stops[stopIndex] =
                            new GradientStop(Target.Stops[stopIndex].Color + ColorDelta,
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
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Undo(IArtContext artContext) { throw new NotImplementedException(); }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (newCommand is ModifyGradientCommand mgc && mgc.Operation == Operation)
            {
                switch (Operation)
                {
                    case GradientOperation.ChangeOffset:
                        return new ModifyGradientCommand(
                            Id, ScalarDelta + mgc.ScalarDelta,
                            StopIndices, Target);
                    case GradientOperation.ChangeColor:
                        return new ModifyGradientCommand(
                            Id, ColorDelta + mgc.ColorDelta,
                            StopIndices, Target);
                    case GradientOperation.ChangeFocus:
                    case GradientOperation.ChangeEnd:
                    case GradientOperation.ChangeStart:
                        return new ModifyGradientCommand(
                            Id, VectorDelta + mgc.VectorDelta,
                            StopIndices, Operation, Target);
                    case GradientOperation.RemoveStop:
                    case GradientOperation.AddStop:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return null;
        }

        public string Description { get; }

        public long Id { get; }

        public long Time { get; } = Service.Time.Now;

        GradientBrushInfo[] IOperationCommand<GradientBrushInfo>.Targets => new[] {Target};

        object[] IOperationCommand.Targets => new object[] {Target};

        #endregion
    }
}