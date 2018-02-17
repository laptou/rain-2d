using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Text;

namespace Ibinimator.Core
{
    public interface IArtContextManager : INotifyPropertyChanged, IAttachment
    {
        IArtContext Context { get; }
    }
}