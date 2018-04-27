using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model
{
    public abstract class ResourceBase : Model, IResource
    {
        public bool Disposed { get; private set; }

        public virtual bool Optimized { get; protected set; }

        #region IResource Members

        public virtual void Dispose() { Disposed = true; }

        #endregion

        /// <inheritdoc />
        public ResourceScope Scope { get; set; }

        /// <inheritdoc />
        public virtual void Optimize(IRenderContext context) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public void AddReference() { ReferenceCount++; }

        /// <inheritdoc />
        public void RemoveReference()
        {
            if(ReferenceCount == 0) throw new InvalidOperationException();

            ReferenceCount--;
        }

        /// <inheritdoc />
        public int ReferenceCount { get; private set; }
    }
}