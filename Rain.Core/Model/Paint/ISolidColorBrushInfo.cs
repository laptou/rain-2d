﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public interface ISolidColorBrushInfo : IBrushInfo
    {
        Color Color { get; set; }
    }
}