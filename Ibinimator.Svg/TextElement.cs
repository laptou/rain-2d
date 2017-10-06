using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class TextElement : ShapeElement, ITextElement
    {
        private readonly List<IInlineTextElement> _spans = new List<IInlineTextElement>();

        #region ITextElement Members

        public IInlineTextElement this[int index]
        {
            get => _spans[index];
            set => _spans[index] = value;
        }

        public void Add(IInlineTextElement item)
        {
            _spans.Add(item);
        }

        public void Clear()
        {
            _spans.Clear();
        }

        public bool Contains(IInlineTextElement item)
        {
            return _spans.Contains(item);
        }

        public void CopyTo(IInlineTextElement[] array, int arrayIndex)
        {
            _spans.CopyTo(array, arrayIndex);
        }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            FontFamily = LazyGet(element, "font-family", true);
            FontSize = LazyGet(element, "font-size", new Length(12, LengthUnit.Pixels), true);
            FontStretch = LazyGet<FontStretch>(element, "font-stretch", inherit: true);
            FontStyle = LazyGet<FontStyle>(element, "font-style", inherit: true);
            FontWeight = LazyGet<FontWeight>(element, "font-weight", inherit: true);
            AlignmentBaseline = LazyGet<AlignmentBaseline>(element, "alignment-baseline", inherit: true);

            Text = element.Value;

            _spans.AddRange(element.Elements().Select(spanElement =>
            {
                IInlineTextElement span;

                switch (spanElement.Name.LocalName)
                {
                    case "tspan":
                        span = new Span();
                        span.FromXml(spanElement, context);
                        break;
                    default:
                        throw new InvalidDataException();
                }

                span.Position = spanElement.ElementsBeforeSelf().Select(x => x.Value.Length).Sum();

                return span;
            }));
        }

        public IEnumerator<IInlineTextElement> GetEnumerator()
        {
            return _spans.GetEnumerator();
        }

        public int IndexOf(IInlineTextElement item)
        {
            return _spans.IndexOf(item);
        }

        public void Insert(int index, IInlineTextElement item)
        {
            _spans.Insert(index, item);
        }

        public bool Remove(IInlineTextElement item)
        {
            return _spans.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _spans.RemoveAt(index);
        }

        public float X { get; set; }

        public float Y { get; set; }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            
            LazySet(element, "alignment-baseline", AlignmentBaseline.Svgify());
            LazySet(element, "baseline-shift", BaselineShift);
            LazySet(element, "font-family", FontFamily);
            LazySet(element, "font-size", FontSize, (12, LengthUnit.Points));
            LazySet(element, "font-stretch", FontStretch);
            LazySet(element, "font-style", FontStyle);
            LazySet(element, "font-weight", FontWeight);

            LazySet(element, "x", X);
            LazySet(element, "y", Y);

            var indices = 
                new Queue<(int Start, IInlineTextElement Span)>(
                    _spans.Select(s => (Start: s.Position, Span: s))
                          .OrderBy(s => s.Start));

            var text = "";

            var i = 0;

            while (i < Text.Length)
            {
                if(indices.Count > 0 && indices.Peek().Start == i)
                {
                    element.Add(new XText(text));

                    text = "";

                    var index = indices.Dequeue();

                    element.Add(index.Span.ToXml(context));

                    i += index.Span.Text.Length;

                    continue;
                }

                text += Text[i];
                i++;
            }

            element.Add(new XText(text));

            return element;
        }

        public FontStyle? FontStyle { get; set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _spans).GetEnumerator();
        }

        public AlignmentBaseline AlignmentBaseline { get; set; }

        public BaselineShift BaselineShift { get; set; }

        public int Count => _spans.Count;

        public string FontFamily { get; set; }

        public Length? FontSize { get; set; } = (12, LengthUnit.Points);

        public FontStretch? FontStretch { get; set; } = Svg.FontStretch.Normal;

        public FontWeight? FontWeight { get; set; } = Svg.FontWeight.Normal;

        public bool IsReadOnly => ((IList<IInlineTextElement>) _spans).IsReadOnly;

        public string Text { get; set; }

        #endregion
    }

    public class Text : TextElement
    {
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Text;

            return element;
        }
    }

    public class Span : TextElement, IInlineTextElement
    {
        public int Position { get; set; }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Tspan;
            
            return element;
        }
    }
}