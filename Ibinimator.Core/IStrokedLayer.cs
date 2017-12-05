using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IStrokedLayer : ILayer
    {
        IPenInfo Stroke { get; set; }

        event EventHandler StrokeChanged;
    }
}