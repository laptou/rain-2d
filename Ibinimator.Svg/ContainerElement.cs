using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class ContainerElement : GraphicalElement, IContainerElement
    {
        private readonly IList<IElement> _list = new List<IElement>();

        #region IContainerElement Members

        public IElement this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
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

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            foreach (var descendant in element.Elements())
            {
                IElement child;

                switch (descendant.Name.LocalName)
                {
                    case "circle":
                        child = new Circle();
                        break;
                    case "ellipse":
                        child = new Ellipse();
                        break;
                    case "g":
                        child = new Group();
                        break;
                    case "line":
                        child = new Line();
                        break;
                    case "path":
                        child = new Path();
                        break;
                    case "polygon":
                        child = new Polygon();
                        break;
                    case "polyline":
                        child = new Polyline();
                        break;
                    case "rect":
                        child = new Rectangle();
                        break;
                    default:
                        continue;
                }

                child.FromXml(descendant, context);

                Add(child);
            }
        }

        public IEnumerator<IElement> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(IElement item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, IElement item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(IElement item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _list).GetEnumerator();
        }

        public int Count => _list.Count;

        public bool IsReadOnly => _list.IsReadOnly;

        #endregion
    }
}