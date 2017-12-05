using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IArtContextManager : INotifyPropertyChanged
    {
        IArtContext Context { get; }
    }
}