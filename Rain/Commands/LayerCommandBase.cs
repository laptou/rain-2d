using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;

namespace Rain.Commands
{
    public abstract class LayerCommandBase<T> : IOperationCommand<T> where T : class, ILayer
    {
        protected LayerCommandBase(long id, IReadOnlyList<T> targets)
        {
            Id = id;
            Targets = targets;
        }

        #region IOperationCommand<T> Members

        public abstract void Do(IArtContext artContext);

        public abstract void Undo(IArtContext artContext);

        public abstract string Description { get; }

        public long Id { get; }

        public IReadOnlyList<T> Targets { get; }

        public long Time { get; } = Utility.Time.Now;

        IReadOnlyList<object> IOperationCommand.Targets => Targets;

        #endregion
    }
}