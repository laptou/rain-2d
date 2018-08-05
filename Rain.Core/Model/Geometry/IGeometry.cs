using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.Geometry
{
    public interface IGeometry : IResource
    {
        #region Measure

        RectangleF Bounds();

        #endregion

        IOptimizedGeometry Optimize();
        IOptimizedGeometry Optimize(IPenInfo pen);

        #region Operations

        IGeometry Copy();
        IGeometry Outline(float width);

        IGeometry Transform(Matrix3x2 transform);

        #endregion

        #region Boolean Operations

        IGeometry Difference(IGeometry other);
        IGeometry Union(IGeometry other);
        IGeometry Intersection(IGeometry other);
        IGeometry Xor(IGeometry other);

        #endregion

        #region Hit Testing

        bool FillContains(float x, float y);
        bool StrokeContains(float x, float y, float width);

        #endregion

        #region Writing

        void Load(IEnumerable<PathInstruction> source);
        IGeometrySink Open();

        #endregion

        #region Reading

        IEnumerable<PathInstruction> Read();
        void Read(IGeometrySink sink);
        IEnumerable<PathNode> ReadNodes();

        #endregion
    }

    public interface IOptimizedGeometry : IGeometry
    {
        GeometryOptimizationMode OptimizationMode { get; }
    }

    [Flags]
    public enum GeometryOptimizationMode
    {
        Stroke = 1 << 0,
        Fill   = 1 << 1
    }
}