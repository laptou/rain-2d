﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Shapes
{
    public interface IInlineTextElement : ITextElement
    {
        int Position { get; set; }
    }
}