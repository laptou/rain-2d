using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Svg.Utilities;

namespace Ibinimator.Svg.Structure
{
    public abstract class GraphicalContainerElement : GraphicalElement, IContainerElement
    {
        private readonly List<IElement> _list = new List<IElement>();

        #region IContainerElement Members

        public IElement this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public void Add(IElement item) { _list.Add(item); }

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

        public void Insert(int index, IElement item) { _list.Insert(index, item); }

        public bool Remove(IElement item) { return _list.Remove(item); }

        public void RemoveAt(int index) { _list.RemoveAt(index); }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            foreach (var child in this)
                element.Add(child?.ToXml(context));

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable) _list).GetEnumerator(); }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList<IElement>) _list).IsReadOnly;

        #endregion
    }

    public abstract class ContainerElement : Element, IContainerElement
    {
        private readonly List<IElement> _list = new List<IElement>();

        #region IContainerElement Members

        public IElement this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public void Add(IElement item) { _list.Add(item); }

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

        public void Insert(int index, IElement item) { _list.Insert(index, item); }

        public bool Remove(IElement item) { return _list.Remove(item); }

        public void RemoveAt(int index) { _list.RemoveAt(index); }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            foreach (var child in this)
                element.Add(child?.ToXml(context));

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable) _list).GetEnumerator(); }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList<IElement>) _list).IsReadOnly;

        #endregion
    }
}