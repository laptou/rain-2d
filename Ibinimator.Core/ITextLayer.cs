using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;

namespace Ibinimator.Core
{
    public interface ITextLayer : IGeometricLayer
    {
        float Baseline { get; }
        string FontFamilyName { get; set; }
        float FontSize { get; set; }
        FontStretch FontStretch { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }
        ObservableList<Format> Formats { get; }
        ObservableList<float> Offsets { get; }

        //ParagraphAlignment ParagraphAlignment { get; set; }
        //TextAlignment TextAlignment { get; set; }
        string Value { get; set; }

        event EventHandler LayoutChanged;

        void ClearFormat();
        Format GetFormat(int position);
        ITextLayout GetLayout(IArtContext ctx);

        void InsertText(int index, string str);
        void RemoveText(int index, int length);
        void SetFormat(Format format);
    }
}