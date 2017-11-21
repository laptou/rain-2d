using System;
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

        public ModifyGradientCommand(long id, float offsetDelta, int[] indices, GradientBrushInfo target)
            : this(id, target)
        {
            OffsetDelta = offsetDelta;
            StopIndices = indices;
        }

        public ModifyGradientCommand(long id, Vector4 colorDelta, int[] indices, GradientBrushInfo target)
            : this(id, target)
        {
            ColorDelta = colorDelta;
            StopIndices = indices;
        }

        public ModifyGradientCommand(long id, Vector2 delta, GradientBrushInfo target)
            : this(id, delta, delta, delta, target)
        {
            StartDelta = EndDelta = FocusDelta = delta;
        }

        public ModifyGradientCommand(long id, Vector2 startDelta, Vector2 endDelta, Vector2 focusDelta, GradientBrushInfo target)
            : this(id, target)
        {
            StartDelta = startDelta;
            EndDelta = endDelta;
            FocusDelta = focusDelta;
        }

        public GradientBrushInfo Target { get; }

        public float OffsetDelta { get; }

        public Vector2 StartDelta { get; }

        public Vector2 EndDelta { get; }

        public Vector2 FocusDelta { get; }

        public Vector4 ColorDelta { get; }

        public IReadOnlyList<int> StopIndices { get; }

        #region IOperationCommand<GradientBrushInfo> Members

        public void Do(IArtContext artContext) { throw new NotImplementedException(); }
        public void Undo(IArtContext artContext) { throw new NotImplementedException(); }
        public IOperationCommand Merge(IOperationCommand newCommand) { throw new NotImplementedException(); }

        public string Description { get; }

        public long Id { get; }

        public long Time { get; } = Service.Time.Now;

        GradientBrushInfo[] IOperationCommand<GradientBrushInfo>.Targets => new[] {Target};

        object[] IOperationCommand.Targets => new object[] {Target};

        #endregion
    }
}