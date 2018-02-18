using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Text;

namespace Ibinimator.Commands
{
    public sealed class ModifyTextCommand : IOperationCommand<ITextContainer>
    {
        private ITextInfo _oldStyle;

        public ModifyTextCommand(long id, ITextContainer target, ITextInfo style)
        {
            Id = id;
            Time = Utility.Time.Now;
            Targets = new[] {target};
            Style = style;
        }

        public ITextInfo Style { get; }

        #region IOperationCommand<ITextContainer> Members

        public void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                target.TextStyle = Style;
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (newCommand is ModifyTextCommand cmd)
                return new ModifyTextCommand(newCommand.Id, cmd.Targets[0], Style);

            return null;
        }

        public void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
                target.TextStyle = _oldStyle;
        }

        public string Description => "Changed format of text";

        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public ITextContainer[] Targets { get; }

        /// <inheritdoc />
        public long Time { get; }

        /// <inheritdoc />
        object[] IOperationCommand.Targets => Targets.ToArray<object>();

        #endregion
    }
}