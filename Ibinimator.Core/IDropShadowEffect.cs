using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core {
    public interface IDropShadowEffect : IEffect
    {
        float Radius { get; set; }
    }
}