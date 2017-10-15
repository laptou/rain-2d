using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IFilledLayer : ILayer
    {
        BrushInfo Fill { get; set; }

        event EventHandler FillChanged;
    }
}