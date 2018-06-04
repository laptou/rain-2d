using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core
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

        IReadOnlyList<object> Targets { get; }

        long Time { get; }

        void Do(IArtContext artContext);

        void Undo(IArtContext artContext);
    }

    public interface IMergeableOperationCommand : IOperationCommand
    {
        IOperationCommand Merge(IOperationCommand newCommand);
    }

    public interface IMergeableOperationCommand<T> : IOperationCommand<T>, IMergeableOperationCommand
    {
        IOperationCommand<T> Merge(IOperationCommand<T> newCommand);
    }

    public interface IOperationCommand<out T> : IOperationCommand
    {
        new IReadOnlyList<T> Targets { get; }
    }
}