using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyFormatCommand : LayerCommandBase<ITextLayer>
    {
        public ApplyFormatCommand(long id, ITextLayer target, Format format) : base(id, new[] {target})
        {
            Format = format;
        }

        public override string Description => "Changed format of range";

        public Format Format { get; }

        private Format[] OldFormats;

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                OldFormats = target.Formats.ToArray();
                target.SetFormat(Format);
                
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

        public override IOperationCommand Merge(IOperationCommand newCommand) { return null; }
    }
}