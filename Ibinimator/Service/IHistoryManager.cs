using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public enum HistoryRecordType
    {
        Transform,
        Delete,
        Add
    }

    public interface IHistoryRecord
    {
        long Id { get; }
        HistoryRecordType Type { get; }
        object Data { get; }
        object Target { get; }
        string Description { get; }

        void Do();
        void Undo();
    }

    public interface IHistoryManager : IArtViewManager
    {
        void Record(IHistoryRecord record);
        void Undo();
        void Redo();

        T Create<T>() where T : IHistoryRecord;

        event EventHandler StackTraversed;
    }
}
