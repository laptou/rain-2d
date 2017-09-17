using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Shared;
using SharpDX.DirectWrite;

namespace Ibinimator.Model
{
    public interface ITextLayer : IFilledLayer, IStrokedLayer
    {
        string FontFamilyName { get; set; }
        float FontSize { get; set; }
        FontStretch FontStretch { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }
        ObservableList<Format> Formats { get; }
        ObservableList<float> Offsets { get; }
        ParagraphAlignment ParagraphAlignment { get; set; }
        TextAlignment TextAlignment { get; set; }
        string Value { get; set; }

        void ClearFormat();
        Format GetFormat(int position);
        TextLayout GetLayout(Factory dwFactory);

        void Insert(int index, string str);
        void Remove(int index, int length);
        void SetFormat(Format format);
    }
}