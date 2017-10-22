using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface IHistoryManager
        : IArtContextManager,
          INotifyCollectionChanged,
          IEnumerable<IOperationCommand<ILayer>>
    {
        /// <summary>
        ///     Returns the last operation that was performed.
        /// </summary>
        IOperationCommand<ILayer> Current { get; }

        /// <summary>
        ///     Returns the position in the stack.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        ///     Fires when Time is changed manually (not when it changes
        ///     automatically, like when the stack is changed).
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
        void Do(IOperationCommand<ILayer> command);

        /// <summary>
        ///     Removes the current operation from the top of the stack.
        ///     This clears the redo stack.
        /// </summary>
        /// <returns></returns>
        IOperationCommand<ILayer> Pop();

        /// <summary>
        ///     Pushes an operation to the top of the stack.
        ///     This clears the redo stack.
        /// </summary>
        /// <param name="command">The command to add.</param>
        void Push(IOperationCommand<ILayer> command);

        /// <summary>
        ///     Increments Time by 1, does the next command in the redo stack.
        /// </summary>
        void Redo();

        /// <summary>
        ///     Removes the current command and pushes this one in its place.
        ///     This clears the redo stack.
        /// </summary>
        /// <param name="command">The command to replace.</param>
        void Replace(IOperationCommand<ILayer> command);

        /// <summary>
        ///     Decrements Time by 1, undoes the next command in the undo stack.
        /// </summary>
        void Undo();
    }
}