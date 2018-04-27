using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model
{
    public interface IResource : IDisposable
    {
        ResourceScope Scope { get; set; }
        bool Optimized { get; }

        void Optimize(IRenderContext context);
        void AddReference();
        void RemoveReference();
        int ReferenceCount { get; }
    }
}