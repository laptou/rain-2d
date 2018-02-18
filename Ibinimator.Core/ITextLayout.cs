using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Geometry;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Model.Text;

namespace Ibinimator.Core
{
    public struct TextPositionMetric
    {
        public float Top;
        public float Baseline;
        public float Height;
        public int   Position;
        public int   Line;
        public float Left;

        public TextPositionMetric(
            float top, float left, float baseline, float height, int position, int line)
        {
            Top = top;
            Baseline = baseline;
            Height = height;
            Position = position;
            Line = line;
            Left = left;
        }
    }

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
        TextPositionMetric MeasurePosition(int index);
        RectangleF[] MeasureRange(int index, int length);
        void RemoveText(int index, int range);

        void SetFormat(Format format);
    }
}