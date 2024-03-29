using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.DocumentGraph
{
    public interface IStroked
    {
        IPenInfo Stroke { get; set; }
    }

    public interface IStrokedLayer : IStroked, ILayer
    {
        event EventHandler StrokeChanged;
    }
}