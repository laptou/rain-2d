using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public abstract class ResourceBase : PropertyChangedBase, IResource
    {
        public bool Disposed { get; private set; }

        #region IResource Members

        public virtual void Dispose() { Disposed = true; }

        public abstract void Optimize();

        #endregion
    }
}