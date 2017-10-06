using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IBrush : IResource, INotifyPropertyChanged
    {
        float Opacity { get; set; }
    }
}