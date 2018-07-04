using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model
{
    public abstract class ResourceBase : Model, IResource
    {
        public bool IsDisposed { get; private set; }

        #region IResource Members

        /// <inheritdoc />
        public event EventHandler Disposed;

        /// <inheritdoc />
        public event EventHandler Disposing;

        public virtual void Dispose()
        {
            Disposing?.Invoke(this, null);
            IsDisposed = true;
            Disposed?.Invoke(this, null);
        }

        /// <inheritdoc />
        public void AddReference() { ReferenceCount++; }

        /// <inheritdoc />
        public void RemoveReference()
        {
            if (ReferenceCount == 0) throw new InvalidOperationException();

            ReferenceCount--;
        }

        public virtual bool Optimized => false;

        /// <inheritdoc />
        public int ReferenceCount { get; private set; }

        /// <inheritdoc />
        public ResourceScope Scope { get; set; }

        #endregion
    }
}