using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IGeometry : IResource
    {
        IGeometry Copy();
        IGeometry Difference(IGeometry other);
        bool FillContains(float x, float y);
        IGeometry Intersection(IGeometry other);
        IGeometrySink Open();
        IGeometry Outline(float width);
        IEnumerable<PathInstruction> Read();
        bool StrokeContains(float x, float y, float width);
        IGeometry Union(IGeometry other);
        IGeometry Xor(IGeometry other);
    }
}