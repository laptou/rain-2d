using System;

namespace Ibinimator.Renderer
{
    public abstract class ResourceBase : PropertyChangedBase, IResource
    {
        public virtual void Dispose()
        {
            Disposed = true;
        }

        public abstract void Optimize();

        public bool Disposed { get; private set; }
    }
}