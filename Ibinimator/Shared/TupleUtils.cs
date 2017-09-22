using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX;

namespace Ibinimator.Shared
{
    public static class TupleUtils
    {
        public static IEnumerable<(T1, T2)> AsTuples<T1, T2>(this Dictionary<T1, T2> dict)
        {
            return dict.Select(kv => (kv.Key, kv.Value));
        }

        public static (float x, float y) ToTuple(this Vector2 vec)
        {
            return (vec.X, vec.Y);
        }

        public static Vector2 ToVector2(this (float x, float y) tuple)
        {
            return new Vector2(tuple.x, tuple.y);
        }
    }
}