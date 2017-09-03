using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public class WeakLock : IDisposable
    {
        private object _object;

        public WeakLock(object obj)
        {
            _object = obj;

            Monitor.TryEnter(obj);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (Monitor.IsEntered(_object))
                Monitor.Exit(_object);

            _object = null;
        }

        #endregion
    }
}