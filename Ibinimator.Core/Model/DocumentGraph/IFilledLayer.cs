using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface IFilledLayer : ILayer
    {
        IBrushInfo Fill { get; set; }

        event EventHandler FillChanged;
    }
}