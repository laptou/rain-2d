using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model
{
    public interface IResource : IDisposable
    {
        bool Optimized { get; }
        int ReferenceCount { get; }
        ResourceScope Scope { get; set; }
        event EventHandler Disposed;

        event EventHandler Disposing;

        void AddReference();
        void RemoveReference();
    }
}