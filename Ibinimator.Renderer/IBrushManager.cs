using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface IBrushManager : IArtContextManager
    {
        BrushInfo Fill { get; set; }
        PenInfo Stroke { get; set; }
    }
}