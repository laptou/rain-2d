using System;

namespace Rain.Utility {
    internal static class Disposer
    {
        public static void SafeDispose<T>(ref T resource) where T : class
        {
            if (resource == null)
                return;

            if (resource is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // shhhh...
                }
            }

            resource = null;
        }
    }
}