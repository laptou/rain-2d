using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.Effects
{
    public interface IDropShadowEffect : IEffect
    {
        Color Color { get; set; }
        float Radius { get; set; }
    }
}