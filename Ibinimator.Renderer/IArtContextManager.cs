using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface IArtContextManager : INotifyPropertyChanged
    {
        IArtContext Context { get; }
    }
}