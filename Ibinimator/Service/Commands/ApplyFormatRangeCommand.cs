using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyFormatRangeCommand : LayerCommandBase<ITextLayer>
    {
        public ApplyFormatRangeCommand(long id,
            ITextLayer target,
            Format[] oldFormat,
            Format[] newFormat) : base(id, new[] {target})
        {
            OldFormats = oldFormat;
            NewFormats = newFormat;
        }

        public override string Description => $"Changed format of range";
        public Format[] NewFormats { get; }

        public Format[] OldFormats { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in NewFormats)
                    target.SetFormat(format);
            }
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in OldFormats)
                    target.SetFormat(format);
            }
        }
    }
}