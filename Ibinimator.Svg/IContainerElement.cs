using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public interface IContainerElement : IContainerElement<IElement>
    {
    }

    public interface IContainerElement<TElement> : IElement, IList<TElement>
    {
    }
}