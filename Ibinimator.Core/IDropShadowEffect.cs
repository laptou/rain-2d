using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IDropShadowEffect : IEffect
    {
        float Radius { get; set; }
        Color Color { get; set; }
    }
}