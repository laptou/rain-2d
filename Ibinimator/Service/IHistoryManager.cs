using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service
{
    public interface IHistoryManager :
        IArtViewManager,
        INotifyCollectionChanged,
        IEnumerable<IOperationCommand<ILayer>>
    {
        IOperationCommand<ILayer> Current { get; }

        long Time { get; set; }

        event EventHandler<long> Traversed;

        void Clear();

        void Do(IOperationCommand<ILayer> command);

        IOperationCommand<ILayer> Pop();

        void Push(IOperationCommand<ILayer> command);

        void Redo();

        void Undo();
    }
}