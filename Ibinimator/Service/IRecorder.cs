using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ibinimator.Model;

namespace Ibinimator.Service
{
    public interface IRecorder<T> : IEnumerable<IRecord<T>> where T : IComparable
    {
        void Base(Layer root);

        void Key<TV>(Layer layer, T time, string property, TV value);

        void Key<TV>(Guid id, T time, string property, TV value);

        void Key(IRecord<T> record);

        Watcher<Guid, Layer> BeginRecord(Layer root);

        IRecord<long> EndRecord(Watcher<Guid, Layer> watcher, T time);
            
        IRecord<T> CurrentRecord { get; }

        T Time { get; set; }

        event EventHandler<T> TimeChanged;
    }

    public interface IRecord<out T> : ISerializable
    {
        Guid LayerId { get; }
        T Id { get; }

        void Apply(Layer layer);
        void Revert(Layer layer);
    }
}
