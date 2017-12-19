using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    /// <summary>
    ///     This is an interface for "operation commands", which are different
    ///     from WPF's UI commands. Operation commands are use to store undo
    ///     state information.
    /// </summary>
    public interface IOperationCommand
    {
        string Description { get; }

        long Id { get; }

        object[] Targets { get; }

        long Time { get; }

        void Do(IArtContext artContext);

        IOperationCommand Merge(IOperationCommand newCommand);

        void Undo(IArtContext artContext);
    }

    public interface IOperationCommand<out T> : IOperationCommand
    {
        new T[] Targets { get; }
    }
}