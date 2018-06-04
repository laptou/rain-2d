using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class TupleUtils
    {
        public static IEnumerable<(T1, T2)> AsTuples<T1, T2>(this IDictionary<T1, T2> dict)
        {
            return dict.Select(kv => (kv.Key, kv.Value));
        }

        public static void Deconstruct(this Vector2 v, out float x, out float y) { (x, y) = (v.X, v.Y); }
    }
}