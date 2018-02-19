using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core.Model
{
    public abstract class ResourceBase : Model, IResource
    {
        public bool Disposed { get; private set; }

        #region IResource Members

        public virtual void Dispose() { Disposed = true; }

        public abstract void Optimize();

        #endregion

        /// <inheritdoc />
        public ResourceScope Scope { get; set; }

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