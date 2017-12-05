using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IFilledLayer : ILayer
    {
        IBrushInfo Fill { get; set; }

        event EventHandler FillChanged;
    }
}