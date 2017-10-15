﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;

namespace Ibinimator.Renderer
{
    public interface ISolidColorBrush : IBrush
    {
        Color Color { get; set; }
    }
}