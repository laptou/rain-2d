using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;

namespace Rain.Commands
{
    public class CompoundCommand : IOperationCommand<IOperationCommand>
    {
        public CompoundCommand(long id, params IOperationCommand[] targets)
        {
            Targets = targets;
            Id = id;
            Time = Utility.Time.Now;
        }

        #region IOperationCommand<IOperationCommand> Members

        public void Do(IArtContext artContext)
        {
            foreach (var command in Targets)
                command.Do(artContext);
        }

        public void Undo(IArtContext artContext)
        {
            foreach (var command in Targets.Reverse())
                command.Do(artContext);
        }

        public string Description => "Performed multiple actions";

        public long Id { get; }

        public IReadOnlyList<IOperationCommand> Targets { get; }

        public long Time { get; }

        IReadOnlyList<object> IOperationCommand.Targets => Targets;

        #endregion
    }
}