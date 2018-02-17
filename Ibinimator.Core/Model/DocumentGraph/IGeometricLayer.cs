using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Geometry;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface IGeometricLayer : IFilledLayer, IStrokedLayer
    {
        event EventHandler GeometryChanged;
        IGeometry GetGeometry(ICacheManager cache);
    }
}