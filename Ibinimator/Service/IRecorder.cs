using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Ibinimator.Model;

namespace Ibinimator.Service
{
    public interface IRecorder<T> : IEnumerable<IRecord<T>> where T : IComparable
    {
        IRecord<T> CurrentRecord { get; }

        T Time { get; set; }

        event EventHandler<T> TimeChanged;
        void Base(Layer root);

        Watcher<Guid, Layer> BeginRecord(Layer root);

        IRecord<long> EndRecord(Watcher<Guid, Layer> watcher, T time);

        void Key<TV>(Layer layer, T time, string property, TV value);

        void Key<TV>(Guid id, T time, string property, TV value);

        void Key(IRecord<T> record);
    }

    public interface IRecord<out T> : ISerializable
    {
        T Id { get; }
        Guid LayerId { get; }

        void Apply(Layer layer);
        void Revert(Layer layer);
    }
}