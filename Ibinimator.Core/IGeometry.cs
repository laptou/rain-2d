using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IGeometry : IResource
    {
        RectangleF Bounds();
        IGeometry Copy();
        IGeometry Difference(IGeometry other);
        bool FillContains(float x, float y);
        IGeometry Intersection(IGeometry other);
        void Load(IEnumerable<PathInstruction> source);
        IGeometrySink Open();
        IGeometry Outline(float width);
        IEnumerable<PathInstruction> Read();
        IEnumerable<PathNode> ReadNodes();
        void Read(IGeometrySink sink);
        bool StrokeContains(float x, float y, float width);
        IGeometry Transform(Matrix3x2 transform);
        IGeometry Union(IGeometry other);
        IGeometry Xor(IGeometry other);
    }
}