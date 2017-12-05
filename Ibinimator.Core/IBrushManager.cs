using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IBrushManager : IArtContextManager
    {
        IBrushInfo Fill { get; set; }
        IPenInfo Stroke { get; set; }
    }
}