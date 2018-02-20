using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core
{
    public interface IRenderable
    {
        void Render(RenderContext target, ICacheManager cache, IViewManager view);
    }
}