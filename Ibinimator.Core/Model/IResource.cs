using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core.Model
{
    public interface IResource : IDisposable
    {
        ResourceScope Scope { get; set; }

        void AddReference();
        void RemoveReference();
        int ReferenceCount { get; }
    }
}