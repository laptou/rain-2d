﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.Geometry
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
        void Read(IGeometrySink sink);
        IEnumerable<PathNode> ReadNodes();
        bool StrokeContains(float x, float y, float width);
        IGeometry Transform(Matrix3x2 transform);
        IGeometry Union(IGeometry other);
        IGeometry Xor(IGeometry other);
        IOptimizedGeometry Optimize();
        IOptimizedGeometry Optimize(IPenInfo pen);
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