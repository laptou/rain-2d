using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Service;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;

namespace Ibinimator.Model
{
    public static class TextLayoutExtensions
    {
        public static void SetFormat(
            this DW.TextLayout layout,
            DW.TextRange range,
            Action<TextRenderer.Format> callback)
        {
            var current = range.StartPosition;
            var end = range.StartPosition + range.Length;

            while (current < end)
            {
                var specifier = layout.GetDrawingEffect(current, out var currentRange) as TextRenderer.Format;

                specifier = specifier == null ? new TextRenderer.Format() : specifier.Clone();

                callback(specifier);

                if (currentRange.Length < 0)
                {
                    layout.SetDrawingEffect(specifier, new DW.TextRange(current, 1));
                    current++;
                    continue;
                }

                var currentEnd = currentRange.StartPosition + currentRange.Length;
                var currentLength = Math.Min(currentEnd, end) - current;

                layout.SetDrawingEffect(specifier, new DW.TextRange(current, currentLength));

                current += currentLength;
            }
        }
    }

    public class TextRenderer : DW.TextRendererBase
    {
        public override Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY,
            D2D.MeasuringMode measuringMode, DW.GlyphRun glyphRun, DW.GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect)
        {
            var context = (Context) clientDrawingContext;
            var format = (Format) clientDrawingEffect;

            var path = new D2D.PathGeometry(context.Factory);
            var sink = path.Open();

            glyphRun.FontFace.GetGlyphRunOutline(
                glyphRun.FontSize,
                glyphRun.Indices,
                glyphRun.Advances,
                glyphRun.Offsets,
                glyphRun.IsSideways,
                glyphRun.BidiLevel % 2 != 0,
                sink);

            sink.Close();

            var geometry = new D2D.TransformedGeometry(
                context.Factory, path,
                Matrix3x2.Translation(baselineOriginX, baselineOriginY))
            {
                Tag = (FillBrush: format?.Fill,
                    StrokeBrush: format?.Stroke,
                    StrokeInfo: format?.StrokeInfo)
            };

            context.Figures.Add(geometry);

            return Result.Ok;
        }

        public override Result DrawStrikethrough(object clientDrawingContext, float baselineOriginX,
            float baselineOriginY,
            ref DW.Strikethrough strikethrough, ComObject clientDrawingEffect)
        {
            DrawLine(
                clientDrawingContext as Context,
                clientDrawingEffect as Format,
                baselineOriginX,
                baselineOriginY + strikethrough.Offset,
                strikethrough.Width,
                strikethrough.Thickness);

            return Result.Ok;
        }

        public override Result DrawUnderline(object clientDrawingContext, float baselineOriginX, float baselineOriginY,
            ref DW.Underline underline,
            ComObject clientDrawingEffect)
        {
            DrawLine(
                clientDrawingContext as Context,
                clientDrawingEffect as Format,
                baselineOriginX,
                baselineOriginY + underline.Offset,
                underline.Width,
                underline.Thickness);

            return Result.Ok;
        }

        public override RawMatrix3x2 GetCurrentTransform(object clientDrawingContext)
        {
            var context = (Context) clientDrawingContext;

            return context.Text.AbsoluteTransform;
        }

        private static void DrawLine(Context context, Format format, float x, float y, float width, float thickness)
        {
            var rect = new D2D.RectangleGeometry(context.Factory, new RectangleF(x, y, width, thickness))
            {
                Tag = (FillBrush: format?.Fill,
                    StrokeBrush: format?.Stroke,
                    StrokeInfo: format?.StrokeInfo)
            };

            context.Figures.Add(rect);
        }

        #region Nested type: Context

        public class Context
        {
            public D2D.Factory Factory { get; set; }
            public List<D2D.Geometry> Figures { get; set; } = new List<D2D.Geometry>();
            public ITextLayer Text { get; set; }
        }

        #endregion

        #region Nested type: Format

        public class Format : ComObject
        {
            public BrushInfo Fill { get; set; }
            public int StrikethroughCount { get; set; }
            public BrushInfo Stroke { get; set; }
            public StrokeInfo StrokeInfo { get; set; }
            public int UnderlineCount { get; set; }

            public Format Clone()
            {
                return new Format
                {
                    Fill = (BrushInfo) Fill?.Clone(),
                    Stroke = (BrushInfo) Stroke?.Clone(),
                    StrokeInfo = (StrokeInfo) StrokeInfo?.Clone(),
                    UnderlineCount = UnderlineCount,
                    StrikethroughCount = StrikethroughCount
                };
            }
        }

        #endregion
    }

    public class Text : Layer, ITextLayer, IGeometricLayer
    {
        public Text()
        {
            FontWeight = DW.FontWeight.Normal;
            FontStretch = DW.FontStretch.Normal;
            FontStyle = DW.FontStyle.Normal;
            FontSize = 12;
            FontFamilyName = "Arial";
            Value = "";
            Formats = new ObservableList<Format>();
            StrokeInfo = new StrokeInfo();
        }

        public bool IsBlock
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override string ElementName => "text";

        public Format GetFormat(int position, out int i)
        {
            i = 0;

            do
            {
                i++;
            } while (i < Formats.Count && Formats[i].Range.StartPosition <= position);

            var format = Formats.ElementAtOrDefault(--i);
            if (format == null) return null;

            return format.Range.StartPosition + format.Range.Length > position
                   && position >= format.Range.StartPosition
                ? format
                : null;
        }

        #region IGeometricLayer Members

        public D2D.Geometry GetGeometry(ICacheManager cache)
        {
            var layout = cache.GetTextLayout(this);

            var factory = cache.ArtView.Direct2DFactory;

            using (var render = new TextRenderer())
            {
                var ctx = new TextRenderer.Context
                {
                    Factory = factory,
                    Text = this
                };

                layout.Draw(ctx, render, 0, 0);

                if (ctx.Figures.Count == 0)
                    return null;

                var target = cache.ArtView.RenderTarget;

                for (var i = 0; i < ctx.Figures.Count; i++)
                {
                    var figure = ctx.Figures[i];

                    var info = (ValueTuple<BrushInfo, BrushInfo, StrokeInfo>) figure.Tag;

                    cache.SetResource(this, i * 2 + 0, info.Item1?.ToDirectX(target));
                    cache.SetResource(this, i * 2 + 1, new Stroke(target, info.Item2, info.Item3));
                }

                return new D2D.GeometryGroup(
                    factory,
                    D2D.FillMode.Winding,
                    ctx.Figures.ToArray());
            }
        }

        #endregion

        #region ITextLayer Members

        public void ClearFormat()
        {
            lock (Formats)
            {
                Formats.Clear();
            }

            RaisePropertyChanged("TextLayout");
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            if (IsBlock)
                return new RectangleF(0, 0, Width, Height);

            var metrics = cache.GetTextLayout(this).Metrics;
            return new RectangleF(metrics.Left, metrics.Top, metrics.Width, metrics.Height);
        }

        public Format GetFormat(int position)
        {
            return GetFormat(position, out var _);
        }

        public DW.TextLayout GetLayout(DW.Factory dwFactory)
        {
            var layout = new DW.TextLayout1((IntPtr) new DW.TextLayout(
                dwFactory,
                Value ?? "",
                new DW.TextFormat(
                    dwFactory,
                    FontFamilyName,
                    FontWeight,
                    FontStyle,
                    FontStretch,
                    FontSize * 96 / 72),
                IsBlock ? Width : float.PositiveInfinity,
                IsBlock ? Height : float.PositiveInfinity))
            {
                TextAlignment = TextAlignment,
                ParagraphAlignment = ParagraphAlignment
            };


            lock (Formats)
            {
                foreach (var format in Formats)
                {
                    var typography = layout.GetTypography(format.Range.StartPosition);

                    if (format.Superscript)
                        typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Superscript, 0));

                    if (format.Subscript)
                        typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Subscript, 0));

                    layout.SetTypography(typography, format.Range);

                    if (format.FontFamilyName != null)
                        layout.SetFontFamilyName(format.FontFamilyName, format.Range);

                    if (format.FontSize != null)
                        layout.SetFontSize(format.FontSize.Value, format.Range);

                    if (format.FontStretch != null)
                        layout.SetFontStretch(format.FontStretch.Value, format.Range);

                    if (format.FontStyle != null)
                        layout.SetFontStyle(format.FontStyle.Value, format.Range);

                    if (format.FontWeight != null)
                        layout.SetFontWeight(format.FontWeight.Value, format.Range);

                    if (format.FontWeight != null)
                        layout.SetFontWeight(format.FontWeight.Value, format.Range);

                    if (format.CharacterSpacing != null)
                        layout.SetCharacterSpacing(
                            format.CharacterSpacing.Value,
                            format.CharacterSpacing.Value,
                            0,
                            format.Range);

                    if (format.Fill != null)
                        layout.SetFormat(format.Range, f => f.Fill = format.Fill);

                    if (format.Stroke != null)
                        layout.SetFormat(format.Range, f => f.Stroke = format.Stroke);

                    if (format.StrokeInfo != null)
                        layout.SetFormat(format.Range, f => f.StrokeInfo = format.StrokeInfo);
                }
            }

            return layout;
        }

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            point = Matrix3x2.TransformPoint(Matrix3x2.Invert(Transform), point);

            cache.GetTextLayout(this)
                .HitTestPoint(point.X, point.Y, out var _, out var isInside);

            return isInside ? this as T : null;
        }

        public void Insert(int position, string str)
        {
            lock (Formats)
            {
                // expand the length of the format
                var format = GetFormat(position, out var index);

                if (format != null)
                    format.Range = new DW.TextRange(
                        format.Range.StartPosition,
                        format.Range.Length + str.Length);

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    format = Formats[index];
                    format.Range = new DW.TextRange(
                        format.Range.StartPosition + str.Length,
                        format.Range.Length);
                    index++;
                }

                Value = Value.Insert(position, str);
            }
        }

        public void Remove(int position, int length)
        {
            lock (Formats)
            {
                var current = position;
                var end = position + length;
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

                    var fstart = format.Range.StartPosition;
                    var len = fstart - position;
                    var fend = fstart + format.Range.Length;

                    if (len <= 0 && fend <= end) Formats.Remove(format);
                    else if (len <= 0 && fend > end) format.Range = new DW.TextRange(fstart, fend - fstart - length);
                    else format.Range = new DW.TextRange(fstart, len);

                    current++;
                }

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    var format = Formats[index];
                    format.Range = new DW.TextRange(
                        format.Range.StartPosition - length,
                        format.Range.Length);
                    index++;
                }

                Value = Value.Remove(position, length);
            }
        }

        public override void Render(D2D.RenderTarget target, ICacheManager cache)
        {
            target.Transform = Transform * target.Transform;

            var geometry = cache.GetGeometry(this);

            if (geometry is D2D.GeometryGroup geometryGroup)
            {
                var geometries = geometryGroup.GetSourceGeometry();

                for (var i = 0; i < geometries.Length; i++)
                {
                    var geom = geometries[i];
                    var fill = cache.GetResource<D2D.Brush>(this, i * 2) ?? cache.GetFill(this);
                    var stroke = cache.GetResource<Stroke>(this, i * 2 + 1) ?? cache.GetStroke(this);

                    if (fill != null)
                        target.FillGeometry(geom, fill);

                    if (stroke?.Brush != null)
                        target.DrawGeometry(
                            geom,
                            stroke.Brush,
                            stroke.Width,
                            stroke.Style);
                }
            }

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
        }

        public void SetFormat(Format format)
        {
            var range = format.Range;
            var start = range.StartPosition;
            var current = range.StartPosition;
            var end = range.StartPosition + range.Length;

            if (start == end) return;

            lock (Formats)
            {
                while (current < end)
                {
                    var oldFormat = GetFormat(current);

                    if (oldFormat != null)
                    {
                        var oldRange = oldFormat.Range;
                        var oStart = oldRange.StartPosition;
                        var oLen = oldRange.Length;
                        var oEnd = oStart + oLen;

                        int nStart;
                        int nLen;

                        var newFormat = oldFormat.Union(format);

                        if (oStart < start) nStart = start;
                        else nStart = oStart;

                        if (oEnd > end) nLen = end - nStart;
                        else nLen = oEnd - nStart;

                        newFormat.Range = new DW.TextRange(nStart, nLen);

                        var nEnd = nStart + nLen;

                        Formats.Remove(oldFormat);
                        Formats.Add(newFormat);

                        if (nStart > oStart)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = new DW.TextRange(oStart, nStart - oStart);
                            Formats.Add(iFormat);
                        }

                        if (nEnd < oEnd)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = new DW.TextRange(nEnd, oEnd - nEnd);
                            Formats.Add(iFormat);
                        }

                        if (start < Math.Min(nStart, oStart))
                        {
                            var iFormat = format.Clone();
                            iFormat.Range = new DW.TextRange(start, Math.Min(nStart, oStart) - start);
                            Formats.Add(iFormat);
                        }

                        start = Math.Max(nEnd, oEnd);

                        current += oLen;
                    }
                    else
                    {
                        current++;
                    }
                }

                if (start < end)
                    Formats.Add(format);

                Trace.WriteLine(string.Join("\n",
                    Formats.Select(f =>
                        $"{f.Range.StartPosition} + {f.Range.Length} -> {f.Range.StartPosition + f.Range.Length}: {f.Fill?.ToString()}")));
            }

            RaisePropertyChanged("TextLayout");
        }

        public override string DefaultName => $@"Text ""{Value}""";

        public BrushInfo FillBrush
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
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public float FontSize
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public DW.FontStretch FontStretch
        {
            get => Get<DW.FontStretch>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public DW.FontStyle FontStyle
        {
            get => Get<DW.FontStyle>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public DW.FontWeight FontWeight
        {
            get => Get<DW.FontWeight>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public ObservableList<Format> Formats
        {
            get => Get<ObservableList<Format>>();
            set => Set(value);
        }

        public override float Height
        {
            get => base.Height;
            set
            {
                base.Height = value;
                RaisePropertyChanged("TextLayout");
            }
        }

        public ObservableList<float> Offsets
        {
            get => Get<ObservableList<float>>();
            set => Set(value);
        }

        public DW.ParagraphAlignment ParagraphAlignment
        {
            get => Get<DW.ParagraphAlignment>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

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

        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public StrokeInfo StrokeInfo
        {
            get => Get<StrokeInfo>();
            set => Set(value);
        }

        public DW.TextAlignment TextAlignment
        {
            get => Get<DW.TextAlignment>();
            set
            {
                Set(value);
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public string Value
        {
            get => Get<string>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(DefaultName));
                RaisePropertyChanged("TextLayout");
                RaisePropertyChanged("Bounds");
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                RaisePropertyChanged("TextLayout");
            }
        }

        #endregion
    }
}