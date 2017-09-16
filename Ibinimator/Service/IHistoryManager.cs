using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public interface IHistoryManager : 
        IArtViewManager, 
        IRecorder<long>, 
        INotifyCollectionChanged
    {
        long NextId { get; }
        void Redo();
        void Undo();
        void Merge();
    }
}