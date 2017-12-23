using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IDropShadowEffect : IEffect
    {
        Color Color { get; set; }
        float Radius { get; set; }
    }
}