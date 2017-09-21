﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public interface IInterpolator<out T>
    {
        T ProvideValue(float time);
    }
}