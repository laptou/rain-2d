using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IBrushInfo
    {
        string Name { get; set; }
        float Opacity { get; set; }
        Matrix3x2 Transform { get; set; }

        IBrush CreateBrush(RenderContext target);
    }
}