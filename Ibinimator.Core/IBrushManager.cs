using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IBrushManager : IArtContextManager
    {
        IReadOnlyCollection<IBrushInfo> BrushHistory { get; }
        void Apply(IBrushInfo fill);
        void Apply(IPenInfo stroke);
        (IBrushInfo Fill, IPenInfo Stroke) Query();
    }
}