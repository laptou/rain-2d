﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IServiceContext
    {
        ICaret CreateCaret(int height);
    }
}