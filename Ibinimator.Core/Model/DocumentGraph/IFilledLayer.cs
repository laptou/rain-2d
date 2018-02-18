using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface IFilled
    {
        IBrushInfo Fill { get; set; }

    }

    public interface IFilledLayer : ILayer, IFilled
    {
        event EventHandler FillChanged;
    }
}