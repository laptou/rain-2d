using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface ITextLayout : IResource
    {
        IGeometry GetGeometryForGlyph(int index);
        IBrush GetBrushForGlyph(int index);
        IPen GetPenForGlyph(int index);
        int GetGlyphCount();
        int GetGlyphCountForGeometry(int index);

        bool Hit(Vector2 point);
        RectangleF Measure();

        void SetFormat(Format format);
        Format GetFormat(int index);

        string Text { get; }

        void InsertText(int index, string text);
        void RemoveText(int index, int range);

        string FontFamily { get; set; }
        float FontSize { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }
        FontStretch FontStretch { get; set; }
        int GetPosition(Vector2 point, out bool trailing);
        RectangleF[] MeasureRange(int index, int length);
        RectangleF MeasurePosition(int index);
    }
}
