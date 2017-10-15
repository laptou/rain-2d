using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IBrush : IResource, INotifyPropertyChanged
    {
        float Opacity { get; set; }
        Matrix3x2 Transform { get; set; }
    }
}