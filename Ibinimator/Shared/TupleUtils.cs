using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace Ibinimator.Shared
{
    public static class TupleUtils
    {
        public static IEnumerable<(T1, T2)> AsTuples<T1, T2>(this Dictionary<T1, T2> dict)
            => dict.Select(kv => (kv.Key, kv.Value));

        public static Vector2 ToVector2(this (float x, float y) tuple) => 
            new Vector2(tuple.x, tuple.y);

        public static (float x, float y) ToTuple(this Vector2 vec) =>
            (vec.X, vec.Y);
    }
}
