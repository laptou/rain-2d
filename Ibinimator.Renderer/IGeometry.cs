using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Renderer
{
    public interface IGeometry : IResource
    {
        RectangleF Bounds();
        IGeometry Copy();
        IGeometry Difference(IGeometry other);
        bool FillContains(float x, float y);
        IGeometry Intersection(IGeometry other);
        IGeometrySink Open();
        IGeometry Outline(float width);
        IEnumerable<PathInstruction> Read();
        void Read(IGeometrySink sink);
        void Load(IEnumerable<PathInstruction> source);
        bool StrokeContains(float x, float y, float width);
        IGeometry Transform(Matrix3x2 transform);
        IGeometry Union(IGeometry other);
        IGeometry Xor(IGeometry other);
    }
}