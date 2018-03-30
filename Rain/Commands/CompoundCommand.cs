using Rain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rain.Commands
{
    public class CompoundCommand : IOperationCommand<IOperationCommand>
    {
        private IReadOnlyList<IOperationCommand> _targets;

        public CompoundCommand(long id, params IOperationCommand[] targets)
        {
            _targets = targets;
            Id = id;
            Time = Utility.Time.Now;
        }

        public string Description => "Performed multiple actions";

        public long Id { get; }

        public IReadOnlyList<IOperationCommand> Targets => _targets;

        public long Time { get; }

        IReadOnlyList<object> IOperationCommand.Targets => Targets;

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
    }
}
