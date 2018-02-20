using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core
{
    public interface IHistoryManager
        : IArtContextManager, INotifyCollectionChanged, IEnumerable<IOperationCommand>
    {
        /// <summary>
        ///     Returns the last operation that was performed.
        /// </summary>
        IOperationCommand Current { get; }

        /// <summary>
        ///     Returns the position in the stack.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        ///     Fires when Time is changed.
        /// </summary>
        event EventHandler<long> Traversed;

        /// <summary>
        ///     Clears the stack.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Does an operation and pushes it on the top of the stack.
        ///     This clears the redo stack.
        /// </summary>
        /// <param name="command">The command to do.</param>
        void Do(IOperationCommand command);

        /// <summary>
        ///     Merges the command with the previous command, provided that they are logically mergeable
        ///     and occurred in quick succession.
        /// </summary>
        /// <param name="newCommand">The command containing the new data.</param>
        /// <param name="timeLimit">The maximum amount of time that can have elapsed for merging to occur.</param>
        void Merge(IOperationCommand newCommand, long timeLimit);

        /// <summary>
        ///     Removes the current operation from the top of the stack.
        ///     This clears the redo stack.
        /// </summary>
        /// <returns></returns>
        IOperationCommand Pop();

        /// <summary>
        ///     Pushes an operation to the top of the stack.
        ///     This clears the redo stack.
        /// </summary>
        /// <param name="command">The command to add.</param>
        void Push(IOperationCommand command);

        /// <summary>
        ///     Increments Time by 1, does the next command in the redo stack.
        /// </summary>
        void Redo();

        /// <summary>
        ///     Removes the current command and pushes this one in its place.
        ///     This clears the redo stack.
        /// </summary>
        /// <param name="command">The command to replace.</param>
        void Replace(IOperationCommand command);

        /// <summary>
        ///     Decrements Time by 1, undoes the next command in the undo stack.
        /// </summary>
        void Undo();
    }
}