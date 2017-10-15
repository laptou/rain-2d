using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Renderer.WPF;
using DW = SharpDX.DirectWrite;

namespace Ibinimator.Renderer.Direct2D
{
    internal sealed class DirectWriteTextLayout : ResourceBase, ITextLayout
    {
        private readonly Direct2DRenderContext _ctx;

        private ObservableList<Format> _formats = new ObservableList<Format>();
        private DW.TextLayout _layout;
        private TextRenderer.Context _textContext;

        public DirectWriteTextLayout(Direct2DRenderContext ctx)
        {
            _ctx = ctx;
        }

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
            } while (index < sorted.Length && sorted[index].Range.Index <= position);

            var format = sorted.ElementAtOrDefault(--index);

            if (format == null) return null;

            return format.Range.Index + format.Range.Length > position
                   && position >= format.Range.Index
                ? format
                : null;
        }

        private void Update()
        {
            _layout?.Dispose();

            _layout = new DW.TextLayout1((IntPtr) new DW.TextLayout(
                    _ctx.FactoryDW,
                    Text ?? "",
                    new DW.TextFormat(
                        _ctx.FactoryDW,
                        FontFamily,
                        (DW.FontWeight) FontWeight,
                        (DW.FontStyle) FontStyle,
                        (DW.FontStretch) FontStretch,
                        FontSize * 96 / 72),
                    IsBlock ? Width : float.PositiveInfinity,
                    IsBlock ? Height : float.PositiveInfinity))
                // ReSharper disable once RedundantEmptyObjectOrCollectionInitializer
                {
                    //TextAlignment = TextAlignment,
                    //ParagraphAlignment = ParagraphAlignment
                };

            lock (_formats)
            {
                foreach (var format in _formats)
                {
                    var typography = _layout.GetTypography(format.Range.Index);

                    if (format.Superscript)
                        typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Superscript, 0));

                    if (format.Subscript)
                        typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Subscript, 0));

                    var range = new DW.TextRange(format.Range.Index, format.Range.Length);

                    _layout.SetTypography(typography, range);

                    if (format.FontFamilyName != null)
                        _layout.SetFontFamilyName(format.FontFamilyName, range);

                    if (format.FontSize != null)
                        _layout.SetFontSize(format.FontSize.Value, range);

                    if (format.FontStretch != null)
                        _layout.SetFontStretch((DW.FontStretch) format.FontStretch.Value, range);

                    if (format.FontStyle != null)
                        _layout.SetFontStyle((DW.FontStyle) format.FontStyle.Value, range);

                    if (format.FontWeight != null)
                        _layout.SetFontWeight((DW.FontWeight) format.FontWeight.Value, range);

                    //if (format.CharacterSpacing != null)
                    //    _layout.SetCharacterSpacing(
                    //        format.CharacterSpacing.Value,
                    //        format.CharacterSpacing.Value,
                    //        0,
                    //        range);

                    if (format.Fill != null)
                        _layout.SetFormat(range, f => f.Fill = format.Fill);

                    if (format.Stroke != null)
                        _layout.SetFormat(range, f => f.Stroke = format.Stroke);
                }
            }

            using (var renderer = new TextRenderer())
            {
                _layout.Draw(_textContext = new TextRenderer.Context(_ctx), renderer, 0, 0);
            }
        }

        #region ITextLayout Members

        public override void Dispose()
        {
            _layout.Dispose();
            _formats = null;

            base.Dispose();
        }

        public IBrush GetBrushForGlyph(int index)
        {
            var j = 0;
            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];
                if (j >= index)
                    return _textContext.Brushes[i];
            }

            return null;
        }

        public Format GetFormat(int index)
        {
            return GetFormat(index, out var _);
        }

        public IGeometry GetGeometryForGlyph(int index)
        {
            var j = 0;
            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];
                if (j >= index)
                    return _textContext.Geometries[i];
            }

            return null;
        }

        public int GetGlyphCount()
        {
            return _textContext.GlyphCount;
        }

        public bool Hit(Vector2 point)
        {
            _layout.HitTestPoint(point.X, point.Y, out var _, out var hit);
            return hit;
        }

        public RectangleF Measure()
        {
            return new RectangleF(_layout.Metrics.Top,
                _layout.Metrics.Left, _layout.Metrics.Width, _layout.Metrics.Height);
        }

        public IPen GetPenForGlyph(int index)
        {
            var j = 0;
            for (var i = 0; i < _textContext.Geometries.Count; i++)
            {
                j += _textContext.CharactersForGeometry[i];
                if (j >= index)
                    return _textContext.Pens[i];
            }

            return null;
        }

        public void InsertText(int position, string text)
        {
            lock (_formats)
            {
                // expand the length of the format
                var format = GetFormat(position, out var index);

                if (format != null)
                    format.Range = (
                        format.Range.Index,
                        format.Range.Length + text.Length);

                index++;

                // offset all of the formats that come after this
                while (index < _formats.Count)
                {
                    format = _formats[index];
                    format.Range = (
                        format.Range.Index + text.Length,
                        format.Range.Length);
                    index++;
                }

                Text = Text.Insert(position, text);
            }

            Update();
        }

        public override void Optimize()
        {
            throw new NotImplementedException();
        }

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

                    if (len <= 0 && fend <= end) _formats.Remove(format);
                    else if (len <= 0 && fend > end) format.Range = (fstart, fend - fstart - range);
                    else format.Range = (fstart, len);

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

                        var newFormat = oldFormat.Union(format);

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
        public int GetPosition(Vector2 point, out bool trailing)
        {
            var metrics = _layout.HitTestPoint(point.X, point.Y, out var isTrailingHit, out var _);
            trailing = isTrailingHit;
            return metrics.TextPosition;
        }

        public RectangleF[] MeasureRange(int index, int length)
        {
            return _layout
                .HitTestTextRange(index, length, 0, 0)
                .Select(m => new RectangleF(m.Left, m.Top, m.Width, m.Height))
                .ToArray();
        }

        public RectangleF MeasurePosition(int index)
        {
            var m = _layout.HitTestTextPosition(index, false, out var _, out var _);
            return new RectangleF(m.Left, m.Top, m.Width, m.Height);
        }

        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }

        public string Text { get; private set; } = "";

        #endregion
    }
}