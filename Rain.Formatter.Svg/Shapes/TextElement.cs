using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model.Measurement;
using Rain.Core.Model.Text;
using Rain.Formatter.Svg.Enums;

namespace Rain.Formatter.Svg.Shapes
{
    public abstract class TextElement : ShapeElement, ITextElement
    {
        private readonly List<IInlineTextElement> _spans = new List<IInlineTextElement>();

        public float X { get; set; }

        public float Y { get; set; }

        #region ITextElement Members

        public IInlineTextElement this[int index]
        {
            get => _spans[index];
            set => _spans[index] = value;
        }

        public void Add(IInlineTextElement item) { _spans.Add(item); }

        public void Clear() { _spans.Clear(); }

        public bool Contains(IInlineTextElement item) { return _spans.Contains(item); }

        public void CopyTo(IInlineTextElement[] array, int arrayIndex)
        {
            _spans.CopyTo(array, arrayIndex);
        }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            FontFamily = LazyGet(element, "font-family", true);
            FontSize = LazyGet(element, "font-size", new Length(12, LengthUnit.Pixels), true);
            FontStretch = LazyGet(element,
                                  "font-stretch",
                                  Core.Model.Text.FontStretch.Normal,
                                  true);
            FontStyle = LazyGet(element, "font-style", Core.Model.Text.FontStyle.Normal, true);
            FontWeight = LazyGet(element, "font-weight", Core.Model.Text.FontWeight.Normal, true);
            AlignmentBaseline =
                LazyGet<AlignmentBaseline>(element, "alignment-baseline", inherit: true);

            Text = element.Value;

            _spans.AddRange(element.Elements()
                                   .Select(spanElement =>
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

                                               span.Position = spanElement
                                                              .ElementsBeforeSelf()
                                                              .Select(x => x.Value.Length)
                                                              .Sum();

                                               return span;
                                           }));
        }

        public IEnumerator<IInlineTextElement> GetEnumerator() { return _spans.GetEnumerator(); }

        public int IndexOf(IInlineTextElement item) { return _spans.IndexOf(item); }

        public void Insert(int index, IInlineTextElement item) { _spans.Insert(index, item); }

        public bool Remove(IInlineTextElement item) { return _spans.Remove(item); }

        public void RemoveAt(int index) { _spans.RemoveAt(index); }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            LazySet(element, "alignment-baseline", AlignmentBaseline, AlignmentBaseline.Auto);
            LazySet(element, "baseline-shift", BaselineShift);
            LazySet(element, "font-family", FontFamily);
            LazySet(element, "font-size", FontSize, (12, LengthUnit.Points));
            LazySet(element,
                    "font-stretch",
                    FontStretch ?? default,
                    Core.Model.Text.FontStretch.Normal);
            LazySet(element, "font-style", FontStyle ?? default, Core.Model.Text.FontStyle.Normal);
            LazySet(element, "font-weight", (int) (FontWeight ?? default), 400);

            LazySet(element, "x", X);
            LazySet(element, "y", Y);

            var indices = new Queue<IInlineTextElement>(_spans.OrderBy(s => s.Position));

            var text = "";

            var i = 0;

            while (i < Text.Length)
            {
                if (indices.Count > 0 &&
                    indices.Peek().Position == i)
                {
                    element.Add(new XText(text));

                    text = "";

                    var span = indices.Dequeue();

                    element.Add(span.ToXml(context));

                    i += span.Text.Length;

                    continue;
                }

                text += Text[i];
                i++;
            }

            element.Add(new XText(text));

            return element;
        }

        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable) _spans).GetEnumerator(); }

        public AlignmentBaseline AlignmentBaseline { get; set; }

        public BaselineShift BaselineShift { get; set; }

        public int Count => _spans.Count;

        public string FontFamily { get; set; }

        public Length? FontSize { get; set; } = (12, LengthUnit.Points);

        public FontStretch? FontStretch { get; set; } = Core.Model.Text.FontStretch.Normal;

        public FontStyle? FontStyle { get; set; }

        public FontWeight? FontWeight { get; set; } = Core.Model.Text.FontWeight.Normal;

        public bool IsReadOnly => ((IList<IInlineTextElement>) _spans).IsReadOnly;

        public string Text { get; set; }

        #endregion
    }
}