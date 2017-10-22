﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;

namespace Ibinimator.Renderer.Model
{
    public class Text : Layer, ITextLayer
    {
        public Text()
        {
            FontWeight = FontWeight.Normal;
            FontStretch = FontStretch.Normal;
            FontStyle = FontStyle.Normal;
            FontSize = 12;
            FontFamilyName = "Arial";
            Value = "";
            Stroke = new PenInfo();

            Formats.CollectionChanged += (s, e) => RaiseLayoutChanged();
        }

        public bool IsBlock
        {
            get => Get<bool>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public DW.ParagraphAlignment ParagraphAlignment
        {
            get => Get<DW.ParagraphAlignment>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public DW.TextAlignment TextAlignment
        {
            get => Get<DW.TextAlignment>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public Format GetFormat(int position, out int index)
        {
            var sorted = Formats.OrderBy(f => f.Range.Index).ToArray();

            index = 0;

            do
            {
                index++;
            } while (index < sorted.Length && sorted[index].Range.Index <= position);

            var format = sorted.ElementAtOrDefault(--index);

            if (format == null) return null;

            return format.Range.Index + format.Range.Length > position && position >= format.Range.Index ?
                format :
                null;
        }

        protected void RaiseFillBrushChanged() { FillChanged?.Invoke(this, null); }

        protected void RaiseGeometryChanged()
        {
            GeometryChanged?.Invoke(this, null);
            RaiseBoundsChanged();
        }

        protected void RaiseLayoutChanged()
        {
            LayoutChanged?.Invoke(this, null);
            RaiseGeometryChanged();
        }

        protected void RaiseStrokeChanged() { StrokeChanged?.Invoke(this, null); }

        protected override void UpdateTransform()
        {
            Transform =
                Matrix3x2.CreateTranslation(-Origin) *
                Matrix3x2.CreateScale(Scale) *
                Matrix3x2.CreateSkew(0, Shear) *
                Matrix3x2.CreateRotation(Rotation) *
                Matrix3x2.CreateTranslation(Origin) *
                Matrix3x2.CreateTranslation(0, -Baseline) *
                Matrix3x2.CreateTranslation(Position);
        }

        #region ITextLayer Members

        public event EventHandler FillChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler LayoutChanged;
        public event EventHandler StrokeChanged;

        public override void ApplyTransform(Matrix3x2 transform)
        {
            base.ApplyTransform(transform);

            Position += new Vector2(0, Baseline);
        }

        public void ClearFormat()
        {
            lock (Formats)
            {
                Formats.Clear();
            }

            RaiseLayoutChanged();
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            if (IsBlock)
                return new RectangleF(0, 0, Width, Height);

            return cache.GetTextLayout(this).Measure();
        }

        public Format GetFormat(int position) { return GetFormat(position, out var _); }

        public IGeometry GetGeometry(ICacheManager cache)
        {
            var layout = cache.GetTextLayout(this);

            var geometries =
                Enumerable
                    .Range(0, layout.GetGlyphCount())
                    .Select(layout.GetGeometryForGlyph)
                    .ToArray();

            return cache.Context.RenderContext.CreateGeometryGroup(geometries);
        }

        public ITextLayout GetLayout(IArtContext ctx)
        {
            var layout = ctx.RenderContext.CreateTextLayout();

            layout.FontSize = FontSize;
            layout.FontStyle = FontStyle;
            layout.FontWeight = FontWeight;
            layout.FontStretch = FontStretch;
            layout.FontFamily = FontFamilyName;
            layout.InsertText(0, Value);

            lock (Formats)
            {
                foreach (var format in Formats)
                    layout.SetFormat(format);
            }

            return layout;
        }

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            point = Vector2.Transform(point, MathUtils.Invert(Transform));

            var layout = cache.GetTextLayout(this);

            return layout.Hit(point) ? this as T : null;
        }

        public void InsertText(int position, string text)
        {
            // Inserting inside of the lock leads to RaiseGeometryChanged()
            // which triggers a re-render, which then asks for the layout
            // which locks the _formats
            Value = Value.Insert(position, text);

            lock (Formats)
            {
                // expand the length of the format
                var format = GetFormat(position, out var index);

                if (format != null)
                    format.Range = (
                        format.Range.Index,
                        format.Range.Length + text.Length);

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    format = Formats[index];
                    format.Range = (
                        format.Range.Index + text.Length,
                        format.Range.Length);
                    index++;
                }
            }

            RaiseLayoutChanged();
        }

        public void RemoveText(int position, int range)
        {
            Value = Value.Remove(position, range);

            lock (Formats)
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

                    if (len <= 0 && fend <= end) Formats.Remove(format);
                    else if (len <= 0 && fend > end) format.Range = (fstart, fend - fstart - range);
                    else format.Range = (fstart, len);

                    current++;
                }

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    var format = Formats[index];
                    format.Range = (format.Range.Index - range, format.Range.Length);
                    index++;
                }
            }
        }

        public override void Render(RenderContext target, ICacheManager cache)
        {
            target.Transform(Transform);

            var layout = cache.GetTextLayout(this);

            for (var i = 0; i < layout.GetGlyphCount();)
            {
                var geom = layout.GetGeometryForGlyph(i);
                var fill = layout.GetBrushForGlyph(i) ?? cache.GetFill(this);
                var pen = layout.GetPenForGlyph(i) ?? cache.GetStroke(this);

                if (fill != null)
                    target.FillGeometry(geom, fill);

                if (pen?.Brush != null)
                    target.DrawGeometry(geom, pen);

                i += layout.GetGlyphCountForGeometry(i);
            }

            target.Transform(MathUtils.Invert(Transform));
        }

        public void SetFormat(Format format)
        {
            var range = format.Range;
            var start = range.Index;
            var current = range.Index;
            var end = range.Index + range.Length;

            if (start == end) return;

            lock (Formats)
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
                            Formats.Add(iFormat);
                        }

                        if (nEnd < oEnd)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = (nEnd, oEnd - nEnd);
                            Formats.Add(iFormat);
                        }

                        if (nStart > start)
                        {
                            var iFormat = newFormat.Clone();
                            iFormat.Range = (start, nStart - start);
                            Formats.Add(iFormat);
                        }

                        current = start = Math.Max(nEnd, oEnd);

                        Formats.Remove(oldFormat);
                        Formats.Add(newFormat);
                    }
                    else
                    {
                        current++;
                    }
                }

                if (start < end)
                {
                    format.Range = (start, end - start);
                    Formats.Add(format);
                }
            }
        }


        public float Baseline { get; private set; }

        public override string DefaultName => $@"Text ""{Value}""";

        public BrushInfo Fill
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public string FontFamilyName
        {
            get => Get<string>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public float FontSize
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public FontStretch FontStretch
        {
            get => Get<FontStretch>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public FontStyle FontStyle
        {
            get => Get<FontStyle>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public FontWeight FontWeight
        {
            get => Get<FontWeight>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public ObservableList<Format> Formats { get; } = new ObservableList<Format>();

        public override float Height
        {
            get => base.Height;
            set
            {
                base.Height = value;
                RaiseLayoutChanged();
            }
        }

        public ObservableList<float> Offsets => Get<ObservableList<float>>();

        public override Vector2 Scale
        {
            get => IsBlock ? Vector2.One : base.Scale;
            set
            {
                if (!IsBlock)
                {
                    base.Scale = value;
                }
                else
                {
                    Width *= value.X;
                    Height *= value.Y;
                }
            }
        }

        public PenInfo Stroke
        {
            get => Get<PenInfo>();
            set
            {
                Set(value);
                RaiseStrokeChanged();
            }
        }

        public string Value
        {
            get => Get<string>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(DefaultName));
                RaiseLayoutChanged();
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                RaiseLayoutChanged();
            }
        }

        #endregion
    }
}