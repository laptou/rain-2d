using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;

namespace Rain.Renderer.Direct2D
{
    public class NullGeometry : ResourceBase, IGeometry
    {

        #region IGeometry Members

        public RectangleF Bounds() { return RectangleF.Empty; }
        public IGeometry Copy() { return new NullGeometry(); }
        public IGeometry Difference(IGeometry other) { return new NullGeometry(); }
        public bool FillContains(float x, float y) { return false; }
        public IGeometry Intersection(IGeometry other) { return new NullGeometry(); }

        public void Load(IEnumerable<PathInstruction> source) { throw new InvalidOperationException(); }

        public IGeometrySink Open() { throw new InvalidOperationException(); }

        public IGeometry Outline(float width) { return new NullGeometry(); }

        public IEnumerable<PathInstruction> Read() { yield break; }
        public void Read(IGeometrySink sink) { }
        public IEnumerable<PathNode> ReadNodes() { yield break; }
        public bool StrokeContains(float x, float y, float width) { return false; }

        public IGeometry Transform(Matrix3x2 transform) { return new NullGeometry(); }

        public IGeometry Union(IGeometry other) { return other.Copy(); }

        public IGeometry Xor(IGeometry other) { return other.Copy(); }

        /// <inheritdoc />
        public IOptimizedGeometry Optimize() { throw new InvalidOperationException(); }

        /// <inheritdoc />
        public IOptimizedGeometry Optimize(IPenInfo pen) { throw new InvalidOperationException(); }

        #endregion
    }
}