using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IGeometrySink : IResource
    {
        void Arc(
            float x, float y, float radiusX, float radiusY, float angle, bool clockwise, bool largeArc);

        void Close(bool open);
        void Cubic(float x, float y, float cx1, float cy1, float cx2, float cy2);
        void Line(float x, float y);
        void Move(float x, float y);
        void Quadratic(float x, float y, float cx1, float cy1);
    }
}