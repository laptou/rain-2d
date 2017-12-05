using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface ITextLayout : IResource
    {
        string FontFamily { get; set; }
        float FontSize { get; set; }
        FontStretch FontStretch { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }

        string Text { get; }
        IBrush GetBrushForGlyph(int index);
        Format GetFormat(int index);
        IGeometry GetGeometryForGlyphRun(int index);
        int GetGlyphCount();
        int GetGlyphCountForGeometry(int index);
        IPen GetPenForGlyph(int index);
        int GetPosition(Vector2 point, out bool trailing);

        bool Hit(Vector2 point);

        void InsertText(int index, string text);
        RectangleF Measure();
        RectangleF MeasurePosition(int index);
        RectangleF[] MeasureRange(int index, int length);
        void RemoveText(int index, int range);

        void SetFormat(Format format);
    }
}