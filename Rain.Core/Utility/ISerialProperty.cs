using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public interface ISerialProperty<out T> : IDisposable, INotifyPropertyChanged
    {
        T Value { get; }
    }
}