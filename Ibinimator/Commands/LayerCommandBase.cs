﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.DocumentGraph;

namespace Ibinimator.Commands
{
    public abstract class LayerCommandBase<T> : IOperationCommand<T> where T : class, ILayer
    {
        protected LayerCommandBase(long id, T[] targets)
        {
            Id = id;
            Targets = targets;
        }

        #region IOperationCommand<T> Members

        public abstract void Do(IArtContext artContext);

        public abstract IOperationCommand Merge(IOperationCommand newCommand);

        public abstract void Undo(IArtContext artContext);

        public abstract string Description { get; }

        public long Id { get; }

        public T[] Targets { get; }

        public long Time { get; } = Utility.Time.Now;

        object[] IOperationCommand.Targets => Targets;

        #endregion
    }
}