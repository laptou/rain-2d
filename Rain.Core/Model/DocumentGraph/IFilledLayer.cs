using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.DocumentGraph
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