using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Text;

namespace Rain.Commands
{
    public sealed class ApplyFormatCommand : LayerCommandBase<ITextLayer>, IMergeableOperationCommand
    {
        private Format[] _oldFormats;

        public ApplyFormatCommand(long id, ITextLayer target, Format format) :
            base(id, new[] {target})
        {
            Format = format;
        }

        public override string Description => "Changed format of range";

        public Format Format { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                _oldFormats = target.Formats.ToArray();
                target.SetFormat(Format);
            }
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (newCommand is ApplyFormatCommand cmd)
                return new ApplyFormatCommand(newCommand.Id,
                                              cmd.Targets[0],
                                              Format.Merge(cmd.Format));

            return null;
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                target.ClearFormat();

                foreach (var format in _oldFormats)
                    target.SetFormat(format);
            }
        }
    }
}