using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public interface IHistoryManager : IArtViewManager, IRecorder<long>, INotifyCollectionChanged
    {
        void Undo();
        void Redo();

        long NextId { get; }
    }
}
