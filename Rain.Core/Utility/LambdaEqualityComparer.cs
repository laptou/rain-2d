using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility {
    public class LambdaEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambda;
        public LambdaEqualityComparer(Func<T, T, bool> lambda) { _lambda = lambda; }

        /// <inheritdoc />
        public bool Equals(T x, T y) => _lambda(x, y);

        /// <inheritdoc />
        public int GetHashCode(T obj) => obj.GetHashCode();
    }
}