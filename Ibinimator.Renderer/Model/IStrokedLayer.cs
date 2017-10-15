using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IStrokedLayer : ILayer
    {
        PenInfo Stroke { get; set; }

        event EventHandler StrokeChanged;
    }
}