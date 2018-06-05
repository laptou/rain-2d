using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class CollectionExtensions
    {
        public static void ReplaceAll<T>(this ICollection<T> collection, IEnumerable<T> range)
        {
            if(collection is ObservableList<T> ol)
            {
                ol.ReplaceRange(range);
                return;
            }

            collection.Clear();
            foreach (var item in range)
                collection.Add(item);
        }

        public static void ReplaceRange<T>(this IList<T> list, IEnumerable<T> range, int offset = 0)
        {
            var index = offset;
            foreach (var item in range)
                list[index++] = item;
        }
    }
}