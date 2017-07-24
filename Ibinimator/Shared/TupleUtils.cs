using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Shared
{
    public static class TupleUtils
    {
        public static IEnumerable<(T1, T2)> AsTuples<T1, T2>(this Dictionary<T1, T2> dict)
            => dict.Select(kv => (kv.Key, kv.Value));
    }
}
