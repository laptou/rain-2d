using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IResource : IDisposable
    {
        void Optimize();
    }
}