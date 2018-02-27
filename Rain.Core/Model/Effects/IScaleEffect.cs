using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Effects
{
    public interface IScaleEffect : IEffect
    {
        ScaleMode ScaleMode { get; set; }
        Vector2 Factor { get; set; }
        Vector2 Origin { get; set; }
        bool SoftBorder { get; set; }
    }

    public enum ScaleMode
    {
        NearestNeighbor,
        Linear,
        Cubic,
        MultiSampleLinear,
        Anisotropic,
        HighQualityCubic,
    }
}