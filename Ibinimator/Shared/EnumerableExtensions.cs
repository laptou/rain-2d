using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Shared
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, Func<T, int, bool> predicate)
        {
            var list = new List<IEnumerable<T>>();
            var set = new List<T>();
            var index = 0;

            using (var enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current, index) && set.Any())
                    {
                        yield return set;
                        set.Clear();
                    }
                    else
                    {
                        set.Add(enumerator.Current);
                    }

                    index++;
                }
            }

            if (set.Any())
                yield return set;
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        {
            return enumerable.Split((t, i) => predicate(t));
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, T element)
        {
            return enumerable.Split((t, i) => t.Equals(element));
        }

        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
            this IEnumerable<KeyValuePair<TKey, TElement>> source)
        {
            return source.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}