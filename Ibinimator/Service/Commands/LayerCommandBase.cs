using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public abstract class LayerCommandBase<T> : IOperationCommand<T> where T : class, ILayer
    {
        protected LayerCommandBase(long id, T[] targets)
        {
            Id = id;
            Targets = targets;
        }

        #region IOperationCommand<T> Members

        public abstract void Do(ArtView artView);

        public abstract void Undo(ArtView artView);

        public abstract string Description { get; }

        public long Id { get; }

        public T[] Targets { get; }

        public long Time { get; } = Service.Time.Now;

        object[] IOperationCommand.Targets => Targets;

        #endregion
    }
}