using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Shared
{
    public static class DictionaryExtensions
    {
        public static TV TryGet<TK, TV>(this Dictionary<TK, TV> dict, TK key)
        {
            return dict.TryGetValue(key, out var value) ? value : default(TV);
        }
    }
}