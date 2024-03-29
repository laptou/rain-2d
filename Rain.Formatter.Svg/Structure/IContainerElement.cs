﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Formatter.Svg.Structure
{
    public interface IContainerElement : IContainerElement<IElement> { }

    public interface IContainerElement<TElement> : IElement, IList<TElement> { }
}