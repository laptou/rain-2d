﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Utility
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> enumerable, Func<T, int, bool> predicate)
        {
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

        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
            this IEnumerable<(TKey, TElement)> source)
        {
            return source.ToDictionary(kv => kv.Item1, kv => kv.Item2);
        }

        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            foreach (var element in enumerable)
            {
                yield return element;

                if (!predicate(element)) break;
            }
        }

        public static IEnumerable<T> SkipUntil<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var yielding = false;

            foreach (var element in enumerable)
            {
                if (yielding) yield return element;

                if (!yielding && !predicate(element)) yielding = true;
            }
        }
    }
}