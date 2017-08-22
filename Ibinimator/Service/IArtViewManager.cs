﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public interface IArtViewManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }
    }
}