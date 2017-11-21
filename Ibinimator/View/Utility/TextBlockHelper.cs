using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;

namespace Ibinimator.View.Utility
{
    public static class TextBlockHelper
    {
        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText",
                typeof(string),
                typeof(TextBlockHelper),
                new UIPropertyMetadata("", FormattedTextChanged));

        public static string GetFormattedText(DependencyObject obj)
        {
            return (string) obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var value = e.NewValue as string;

            if (sender is TextBlock textBlock)
            {
                textBlock.Inlines.Clear();
                textBlock.Inlines.Add(Process(value));
            }
        }

        private static void InternalProcess(Span span, XmlNode xmlNode)
        {
            foreach (XmlNode child in xmlNode)
                switch (child)
                {
                    case XmlText _:
                        span.Inlines.Add(new Run(child.InnerText));
                        break;
                    case XmlElement _:
                    {
                        switch (child.Name.ToUpper())
                        {
                            case "B":
                            case "BOLD":
                            {
                                var boldSpan = new Span();
                                InternalProcess(boldSpan, child);
                                var bold = new Bold(boldSpan);
                                span.Inlines.Add(bold);
                                break;
                            }
                            case "I":
                            case "ITALIC":
                            {
                                var italicSpan = new Span();
                                InternalProcess(italicSpan, child);
                                var italic = new Italic(italicSpan);
                                span.Inlines.Add(italic);
                                break;
                            }
                            case "U":
                            case "UNDERLINE":
                            {
                                var underlineSpan = new Span();
                                InternalProcess(underlineSpan, child);
                                var underline = new Underline(underlineSpan);
                                span.Inlines.Add(underline);
                                break;
                            }
                        }

                        break;
                    }
                }
        }

        private static Inline Process(string value)
        {
            var doc = new XmlDocument();
            doc.LoadXml($"<span>{value}</span>");

            var span = new Span();
            InternalProcess(span, doc.ChildNodes[0]);

            return span;
        }
    }
}