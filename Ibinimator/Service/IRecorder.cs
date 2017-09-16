using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Ibinimator.Model;

namespace Ibinimator.Service
{
    public interface IRecorder<TK> : IEnumerable<IRecord<TK>> where TK : IComparable
    {
        IRecord<TK> CurrentRecord { get; }

        TK Time { get; set; }

        void Clear();

        void BeginRecord();

        void EndRecord(string desc = null);
    }

    public interface IHistoryRecord<out TK, in TV> : IRecord<TK, TV> where TK : IComparable
    {
        string Description { get; }

        long Time { get; }
    }

    public interface IRecord<out TK, in TV> : IRecord<TK> where TK : IComparable
    {
        void Apply(TV target);

        void Revert(TV target);
    }

    public interface IRecord<out TK> where TK : IComparable
    {
        TK Id { get; }

        IDictionary OldProperties { get; }

        IDictionary NewProperties { get; }

        void Apply(object target);

        void Revert(object target);
    }
}