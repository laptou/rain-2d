﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IPen : IDisposable, INotifyPropertyChanged
    {
        IBrush Brush { get; set; }
        IList<float> Dashes { get; }
        float DashOffset { get; set; }
        LineCap LineCap { get; set; }
        LineJoin LineJoin { get; set; }
        float MiterLimit { get; set; }
        float Width { get; set; }
    }
}