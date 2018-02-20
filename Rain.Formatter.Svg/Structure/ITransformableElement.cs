using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Formatter.Svg.Structure
{
    public interface ITransformableElement
    {
        Matrix3x2 Transform { get; set; }
    }
}