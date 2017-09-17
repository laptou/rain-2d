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
        string Value { get; set; }
        ObservableList<Format> Formats { get; }
        TextLayout GetLayout(Factory dwFactory);

        void Insert(int index, string str);
        void Remove(int index, int length);

        Format GetFormat(int position);
        void SetFormat(Format format);
        void ClearFormat();
    }
}