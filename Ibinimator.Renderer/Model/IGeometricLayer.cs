using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IGeometricLayer : IFilledLayer, IStrokedLayer
    {
        event EventHandler GeometryChanged;
        IGeometry GetGeometry(ICacheManager cache);
    }
}