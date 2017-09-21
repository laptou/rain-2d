using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public abstract class ContainerElementBase : GraphicalElementBase, IContainerElement
    {
        private readonly IList<IElement> _list = new List<IElement>();

        public IEnumerator<IElement> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _list).GetEnumerator();
        }

        public void Add(IElement item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(IElement item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(IElement[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(IElement item)
        {
            return _list.Remove(item);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => _list.IsReadOnly;

        public int IndexOf(IElement item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, IElement item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public IElement this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}