using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IRenderable
    {
        void Render(RenderContext target, ICacheManager cache, IViewManager view);
    }
}