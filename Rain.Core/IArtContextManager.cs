using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Text;

namespace Rain.Core
{
    public interface IArtContextManager : INotifyPropertyChanged, IAttachment
    {
        IArtContext Context { get; }
    }
}