using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Effects
{
    public interface IGlowEffect : IEffect
    {
        float Radius { get; set; }
    }
}