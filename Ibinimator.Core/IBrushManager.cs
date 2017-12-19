using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IBrushManager : IArtContextManager
    {
        IReadOnlyCollection<IBrushInfo> BrushHistory { get; }
        IBrushInfo                      Fill         { get; set; }
        IPenInfo                        Stroke       { get; set; }
        void                            ApplyFill();
        void                            ApplyStroke();
        void                            Query();
    }
}