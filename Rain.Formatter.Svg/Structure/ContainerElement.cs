using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Structure
{
    public abstract class ContainerElement : Element, IContainerElement
    {
        private readonly List<IElement> _list = new List<IElement>();

        public IElement this[string id] => _list.First(e => e.Id == id);

        #region IContainerElement Members

        IElement IList<IElement>.this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public void Add(IElement item)
        {
            _list.Add(item);
            item.Parent = this;
        }

        public void Clear() { _list.Clear(); }

        public bool Contains(IElement item) { return _list.Contains(item); }

        public void CopyTo(IElement[] array, int arrayIndex) { _list.CopyTo(array, arrayIndex); }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            _list.AddRange(element.Elements().Select(x => X.FromXml(x, context)));
        }

        public IEnumerator<IElement> GetEnumerator() { return _list.GetEnumerator(); }

        public int IndexOf(IElement item) { return _list.IndexOf(item); }

        public void Insert(int index, IElement item)
        {
            item.Parent = this;
            _list.Insert(index, item);
        }

        public bool Remove(IElement item)
        {
            item.Parent = null;

            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list[index].Parent = null;
            _list.RemoveAt(index);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Add(this.Select(c => c?.ToXml(context)));

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable) _list).GetEnumerator(); }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList<IElement>) _list).IsReadOnly;

        #endregion
    }
}