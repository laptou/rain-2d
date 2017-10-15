using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IGeometricLayer : IFilledLayer, IStrokedLayer
    {
        IGeometry GetGeometry(ICacheManager cache);

        event EventHandler GeometryChanged;
    }
}