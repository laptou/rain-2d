using System;
using System.Collections.Generic;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
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

        void Do(ArtView artView);

        void Undo(ArtView artView);
    }

    public interface IOperationCommand<out T> : IOperationCommand
    {
        new T[] Targets { get; }
    }
}