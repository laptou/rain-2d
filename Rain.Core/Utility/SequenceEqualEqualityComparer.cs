using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public class SequenceEqualEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public static readonly SequenceEqualEqualityComparer<T> Instance = new SequenceEqualEqualityComparer<T>();

        private readonly IEqualityComparer<T> _itemComparer;
        public SequenceEqualEqualityComparer() { }
        public SequenceEqualEqualityComparer(IEqualityComparer<T> itemComparer) { _itemComparer = itemComparer; }

        #region IEqualityComparer<IEnumerable<T>> Members

        /// <inheritdoc />
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == null &&
                y == null) return true;

            if (x == null ||
                y == null)
                return false;

            return _itemComparer == null ? x.SequenceEqual(y) : x.SequenceEqual(y, _itemComparer);
        }

        /// <inheritdoc />
        public int GetHashCode(IEnumerable<T> obj) { return obj.GetHashCode(); }

        #endregion
    }
}