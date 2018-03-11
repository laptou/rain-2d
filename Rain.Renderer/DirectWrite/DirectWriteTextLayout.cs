using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Core.Utility;

using DW = SharpDX.DirectWrite;
using FontStretch = Rain.Core.Model.Text.FontStretch;
using FontStyle = Rain.Core.Model.Text.FontStyle;
using FontWeight = Rain.Core.Model.Text.FontWeight;

namespace Rain.Renderer.Direct2D
{
    internal sealed class DirectWriteTextLayout : ResourceBase, ITextLayout
    {
        public const     string                 FallbackFont = "Arial";
        private readonly Direct2DRenderContext  _ctx;
        private          DW.TextLayout          _dwLayout;
        private          ObservableList<Format> _formats = new ObservableList<Format>();
        private          TextRenderer.Context   _textContext;

        public DirectWriteTextLayout(Direct2DRenderContext ctx) { _ctx = ctx; }
        public float FontHeight { get; private set; }

        public float Height { get; set; }

        public bool IsBlock { get; set; }

        public float Width { get; set; }

        public Format GetFormat(int position, out int index)
        {
            var sorted = _formats.OrderBy(f => f.Range.Index).ToArray();

            index = 0;

            do
            {
                index++;
            }
            while (index < sorted.Length && sorted[index].Range.Index <= position);

            var format = sorted.ElementAtOrDefault(--index);

            if (format == null) return null;

            var end = format.Range.Index + format.Range.Length;

            return end > position && position >= format.Range.Index ? format : null;
        }

        private void Update()
        {
            _dwLayout?.Dispose();

            var dwFormat = new DW.TextFormat(_ctx.FactoryDW,
                                             FontFamily,
                                             (DW.FontWeight) FontWeight,
                                             (DW.FontStyle) FontStyle,
                                             (DW.FontStretch) FontStretch,
                                             FontSize * 96 / 72);

            // calculate line height to use for offset
            var families = dwFormat.FontCollection;
            if (!families.FindFamilyName(FontFamily, out var familyIndex))
                familyIndex = 0;

            var family = families.GetFontFamily(familyIndex);
            var font = family.GetFirstMatchingFont((DW.FontWeight) FontWeight,
                                                   (DW.FontStretch) FontStretch,
                                                   (DW.FontStyle) FontStyle);
            FontHeight = (float) (font.Metrics.Ascent + font.Metrics.LineGap) /
                         font.Metrics.DesignUnitsPerEm * dwFormat.FontSize;

            _dwLayout =
                new DW.TextLayout1(
                        (IntPtr) new DW.TextLayout(_ctx.FactoryDW,
                                                   Text ?? "",
                                                   dwFormat,
                                                   Width,
                                                   Height))
                    {
                        //TextAlignment = TextAlignment,
                        //ParagraphAlignment = ParagraphAlignment
                        WordWrapping = IsBlock ? DW.WordWrapping.Wrap : DW.WordWrapping.NoWrap
                    };

            lock (_formats)
            {
                foreach (var format in _formats)
                {
                    var typography = _dwLayout.GetTypography(format.Range.Index);

                    if (format.Superscript)
                        typography.AddFontFeature(
                            new DW.FontFeature(DW.FontFeatureTag.Superscript, 0));

                    if (format.Subscript)
                        typography.AddFontFeature(
                            new DW.FontFeature(DW.FontFeatureTag.Subscript, 0));

                    var range = new DW.TextRange(format.Range.Index, format.Range.Length);

                    _dwLayout.SetTypography(typography, range);

                    if (format.FontFamily != null)
                        _dwLayout.SetFontFamilyName(format.FontFamily, range);

                    if (format.FontSize != null)
                        _dwLayout.SetFontSize(format.FontSize.Value, range);

                    if (format.FontStretch != null)
                        _dwLayout.SetFontStretch((DW.FontStretch) format.FontStretch.Value, range);

                    if (format.FontStyle != null)
                        _dwLayout.SetFontStyle((DW.FontStyle) format.FontStyle.Value, range);

                    if (format.FontWeight != null)
                        _dwLayout.SetFontWeight((DW.FontWeight) format.FontWeight.Value, range);

                    //if (format.CharacterSpacing != null)
                    //    _layout.SetCharacterSpacing(
                    //        format.CharacterSpacing.Value,
                    //        format.CharacterSpacing.Value,
                    //        0,
                    //        range);

                    if (format.Fill != null)
                        _dwLayout.SetFormat(range, f => f.Fill = format.Fill as BrushInfo);

                    if (format.Stroke != null)
                        _dwLayout.SetFormat(range, f => f.Stroke = format.Stroke as PenInfo);
                }
            }

            using (var renderer = new TextRenderer())
            {
                _dwLayout.Draw(_textContext = new TextRenderer.Context(_ctx),
                               renderer,
                               0,
                               -FontHeight);
            }
        }

        #region ITextLayout Members

        public override void Dispose()
        {
            _dwLayout.Dispose();
            _formats = null;

            base.Dispose();
        }

        public IBrush GetBrushForGlyph(int index)
        {
            var j = 0;

            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];

                if (j >= index + 1)
                    return _textContext.Brushes[i];
            }

            return null;
        }

        public Format GetFormat(int index) { return GetFormat(index, out var _); }

        public IGeometry GetGeometryForGlyphRun(int index)
        {
            var j = 0;

            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];

                if (j >= index + 1)
                    return _textContext.Geometries[i];
            }

            return null;
        }

        public int GetGlyphCount() { return _textContext.GlyphCount; }

        public int GetGlyphCountForGeometry(int index)
        {
            var j = 0;

            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];

                if (j >= index + 1)
                    return _textContext.CharactersForGeometry[i];
            }

            return -1;
        }

        public IPen GetPenForGlyph(int index)
        {
            var j = 0;

            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];

                if (j >= index + 1)
                    return _textContext.Pens[i];
            }

            return null;
        }

        public int GetPosition(Vector2 point, out bool trailing)
        {
            var metrics =
                _dwLayout.HitTestPoint(point.X, point.Y, out var isTrailingHit, out var _);
            trailing = isTrailingHit;

            return metrics.TextPosition;
        }

        public bool Hit(Vector2 point)
        {
            _dwLayout.HitTestPoint(point.X, point.Y + FontHeight, out var _, out var hit);

            return hit;
        }

        public void InsertText(int position, string text)
        {
            lock (_formats)
            {
                // expand the length of the format
                var format = GetFormat(position, out var index);

                if (format != null)
                    format.Range = (format.Range.Index, format.Range.Length + text.Length);

                index++;

                // offset all of the formats that come after this
                while (index < _formats.Count)
                {
                    format = _formats[index];
                    format.Range = (format.Range.Index + text.Length, format.Range.Length);
                    index++;
                }

                Text = Text.Insert(position, text);
            }

            Update();
        }

        public RectangleF Measure()
        {
            var oMetrics = _dwLayout.OverhangMetrics;
            var metrics = _dwLayout.Metrics;

            var rect = new RectangleF(0, -FontHeight, metrics.LayoutWidth, metrics.LayoutHeight);

            if (!IsBlock)
            {
                rect.Left = -oMetrics.Left;
                rect.Top = -oMetrics.Top;
                rect.Right = oMetrics.Right;
                rect.Bottom = oMetrics.Bottom;

                rect.Offset(0, -FontHeight);
            }

            return rect;
        }

        public TextPositionMetric MeasurePosition(int index)
        {
            var m = _dwLayout.HitTestTextPosition(index, false, out var _, out var _);

            return new TextPositionMetric(m.Top - FontHeight,
                                          m.Left,
                                          FontHeight,
                                          m.Height,
                                          index,
                                          0);
        }

        public RectangleF[] MeasureRange(int index, int length)
        {
            return _dwLayout.HitTestTextRange(index, length, 0, 0)
                            .Select(m => new RectangleF(m.Left,
                                                        m.Top - FontHeight,
                                                        m.Width,
                                                        m.Height))
                            .ToArray();
        }

        public override void Optimize() { throw new NotImplementedException(); }

        public void RemoveText(int position, int range)
        {
            lock (_formats)
            {
                var current = position;
                var end = position + range;
                var index = 0;

                while (current < end)
                {
                    // expand the length of the format
                    var format = GetFormat(current, out var idx);

                    if (format == null)
                    {
                        current++;

                        continue;
                    }

                    index = idx;

                    var fstart = format.Range.Index;
                    var len = fstart - position;
                    var fend = fstart + format.Range.Length;

                    if (len <= 0 &&
                        fend <= end) _formats.Remove(format);
                    else if (len <= 0 &&
                             fend > end)
                        format.Range = (fstart, fend - fstart - range);
                    else
                        format.Range = (fstart, len);

                    current++;
                }

                index++;

                // offset all of the formats that come after this
                while (index < _formats.Count)
                {
                    var format = _formats[index];
                    format.Range = (format.Range.Index - range, format.Range.Length);
                    index++;
                }

                Text = Text.Remove(position, range);
            }

            Update();
        }

        public void SetFormat(Format format)
        {
            var range = format.Range;
            var start = range.Index;
            var current = range.Index;
            var end = range.Index + range.Length;

            if (start == end) return;

            lock (_formats)
            {
                while (current < end)
                {
                    var oldFormat = GetFormat(current);

                    if (oldFormat != null)
                    {
                        var oldRange = oldFormat.Range;
                        var oStart = oldRange.Index;
                        var oLen = oldRange.Length;
                        var oEnd = oStart + oLen;

                        int nStart;
                        int nLen;

                        var newFormat = oldFormat.Merge(format);

                        if (oStart < start) nStart = start;
                        else nStart = oStart;

                        if (oEnd > end) nLen = end - nStart;
                        else nLen = oEnd - nStart;

                        newFormat.Range = (nStart, nLen);

                        var nEnd = nStart + nLen;

                        if (start > oStart)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = (oStart, start - oStart);
                            _formats.Add(iFormat);
                        }

                        if (nEnd < oEnd)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = (nEnd, oEnd - nEnd);
                            _formats.Add(iFormat);
                        }

                        if (nStart > start)
                        {
                            var iFormat = newFormat.Clone();
                            iFormat.Range = (start, nStart - start);
                            _formats.Add(iFormat);
                        }

                        current = start = Math.Max(nEnd, oEnd);

                        _formats.Remove(oldFormat);
                        _formats.Add(newFormat);
                    }
                    else
                    {
                        current++;
                    }
                }

                if (start < end)
                {
                    format.Range = (start, end - start);
                    _formats.Add(format);
                }
            }

            Update();
        }

        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; }
        public FontStretch FontStretch { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }

        public string Text { get; private set; } = "";

        #endregion
    }
}