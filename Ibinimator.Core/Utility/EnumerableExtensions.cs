using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ibinimator.Core.Utility
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

        public static IEnumerable<T> Cycle<T>(
            this IEnumerable<T> enumerable,
            int elements)
        {
            var enumerable1 = enumerable as IList<T> ?? enumerable.ToList();

            return Enumerable.Concat(enumerable1.Skip(elements),
                                     enumerable1.Take(elements - 1));
        }

        public static IEnumerable<T> Replace<T>(
            this IEnumerable<T> enumerable,
            T oldItem,
            T newItem)
        {
            foreach (var item in enumerable)
            {
                if (item.Equals(oldItem)) yield return newItem;
                else yield return item;
            }
        }
    }

    public static class StringExtensions
    {
        public static string ToTitle(this string s)
        {
            return Thread.CurrentThread.CurrentUICulture.TextInfo.ToTitleCase(s);
        }

        public static string Pascalize(this string s)
        {
            return s.ToTitle().Replace("-", "");
        }

        public static string Truncate(this string s, int length, string terminator = "...")
        {
            if (s.Length < length)
                return s;

            return s.Substring(0, length) + terminator;
        }
    }
}