using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Text;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public interface ITextLayer : ITextContainerLayer, IGeometricLayer
    {
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