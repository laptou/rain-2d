using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;

using D2D1 = SharpDX.Direct2D1;

namespace Rain.Renderer.Direct2D
{
    internal class RealizedGeometry : ResourceBase, IOptimizedGeometry
    {
        private const string
            ErrorMessage = "This is a geometry realization and cannot be queried.";

        private readonly D2D1.RenderTarget        _target;
        private          D2D1.GeometryRealization _geom;

        public static implicit operator D2D1.GeometryRealization(RealizedGeometry geometry)
        {
            return geometry?._geom;
        }


        public RealizedGeometry(D2D1.RenderTarget target, D2D1.Geometry geom)
        {
            _target = target;

            var dc = target.QueryInterface<D2D1.DeviceContext1>();

            lock (geom)
            {
                if (geom.IsDisposed)
                    throw new NullReferenceException(nameof(geom));

                _geom = new D2D1.GeometryRealization(dc, geom, 0.25f);
            }

            OptimizationMode = GeometryOptimizationMode.Fill;
        }

        public RealizedGeometry(D2D1.RenderTarget target, D2D1.Geometry geom, IPenInfo pen)
        {
            _target = target;

            var dc = target.QueryInterface<D2D1.DeviceContext1>();

            var factory = target.Factory.QueryInterface<D2D1.Factory1>();
            var props = new D2D1.StrokeStyleProperties1
            {
                TransformType = D2D1.StrokeTransformType.Fixed,
                DashCap = (D2D1.CapStyle) pen.LineCap,
                StartCap = (D2D1.CapStyle) pen.LineCap,
                EndCap = (D2D1.CapStyle) pen.LineCap,
                LineJoin = (D2D1.LineJoin) pen.LineJoin,
                DashStyle = D2D1.DashStyle.Solid,
                DashOffset = pen.DashOffset,
                MiterLimit = pen.MiterLimit
            };

            D2D1.StrokeStyle1 style;

            if (pen.Dashes.Count == 0)
            {
                style = new D2D1.StrokeStyle1(factory, props);
            }
            else
            {
                props.DashStyle = D2D1.DashStyle.Custom;
                style = new D2D1.StrokeStyle1(factory, props, pen.Dashes.ToArray());
            }

            lock (geom)
            {
                if (geom.IsDisposed)
                    throw new NullReferenceException(nameof(geom));

                _geom = new D2D1.GeometryRealization(dc, geom, 0.25f, pen.Width, style);
            }

            style.Dispose();

            OptimizationMode = GeometryOptimizationMode.Stroke;
        }

        #region IOptimizedGeometry Members

        /// <inheritdoc />
        public RectangleF Bounds() { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Copy() { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Difference(IGeometry other) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public bool FillContains(float x, float y) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Intersection(IGeometry other) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public void Load(IEnumerable<PathInstruction> source) { }

        /// <inheritdoc />
        public IGeometrySink Open() { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IOptimizedGeometry Optimize()
        {
            if (OptimizationMode == GeometryOptimizationMode.Fill)
                return this;

            throw new InvalidOperationException("This geometry is already optimized.");
        }

        /// <inheritdoc />
        public IOptimizedGeometry Optimize(IPenInfo pen)
        {
            if (OptimizationMode == GeometryOptimizationMode.Stroke)
                return this;

            throw new InvalidOperationException("This geometry is already optimized.");
        }

        /// <inheritdoc />
        public IGeometry Outline(float width) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IEnumerable<PathInstruction> Read() { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public void Read(IGeometrySink sink) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IEnumerable<PathNode> ReadNodes() { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public bool StrokeContains(float x, float y, float width) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Transform(Matrix3x2 transform) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Union(IGeometry other) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public IGeometry Xor(IGeometry other) { throw new InvalidOperationException(ErrorMessage); }

        /// <inheritdoc />
        public GeometryOptimizationMode OptimizationMode { get; }

        /// <inheritdoc />
        public override bool Optimized => true;

        #endregion
    }
}