using System.Collections.Specialized;

namespace Ibinimator.Service
{
    public interface IHistoryManager : IArtViewManager, IRecorder<long>, INotifyCollectionChanged
    {
        void Undo();
        void Redo();

        long NextId { get; }
    }
}
