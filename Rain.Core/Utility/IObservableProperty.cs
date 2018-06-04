using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public interface IObservableProperty<out T> : IObservable<T>, IDisposable
    {
        T Value { get; }
    }
}