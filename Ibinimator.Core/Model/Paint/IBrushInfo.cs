using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model.Paint
{
    public interface IBrushInfo : IResource, IModel
    {
        string Name { get; set; }
        float Opacity { get; set; }
        Matrix3x2 Transform { get; set; }

        IBrush CreateBrush(RenderContext target);
    }
}