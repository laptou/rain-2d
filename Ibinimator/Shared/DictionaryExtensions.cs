using System.Collections.Generic;

namespace Ibinimator.Shared
{
    public static class DictionaryExtensions
    {
        public static TV TryGet<TK, TV>(this Dictionary<TK, TV> dict, TK key) =>
            dict.TryGetValue(key, out TV value) ? value : default(TV);
    }
}
