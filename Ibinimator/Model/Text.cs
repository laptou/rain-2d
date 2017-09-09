using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Service;
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

                var currentEnd = currentRange.StartPosition + currentRange.Length;
                var currentLength = Math.Min(currentEnd, end) - current;

                layout.SetDrawingEffect(specifier, new DW.TextRange(current, currentLength));

                current++;
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
            
            var path = new D2D.PathGeometry(context.Target.Factory);
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
                context.Target.Factory, path,
                Matrix3x2.Translation(baselineOriginX, baselineOriginY));

            var fill = format?.Fill ?? context.BaseFill;

            if (fill != null)
                context.Target.FillGeometry(geometry, fill);

            var stroke = format?.Stroke ?? context.BaseStroke;
            var width = format?.StrokeWidth ?? context.BaseStrokeWidth;
            var style = format?.StrokeStyle ?? context.BaseStrokeStyle;

            if (stroke != null && width > 0)
                if (style != null)
                    context.Target.DrawGeometry(geometry, stroke, width, style);
                else
                    context.Target.DrawGeometry(geometry, stroke, width);

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
            return context.Target.Transform;
        }

        public override float GetPixelsPerDip(object clientDrawingContext)
        {
            var context = (Context) clientDrawingContext;
            return context.Target.DotsPerInch.Height / 96f;
        }

        private static void DrawLine(Context context, Format format, float x, float y, float width, float thickness)
        {
            var rect = new RectangleF(x, y, width, thickness);

            var fill = format.Fill ?? context.BaseFill;

            if (fill != null)
                context.Target.FillRectangle(rect, fill);

            var stroke = format.Stroke ?? context.BaseStroke;
            var sWidth = format.StrokeWidth ?? context.BaseStrokeWidth;
            var sStyle = format.StrokeStyle ?? context.BaseStrokeStyle;

            if (stroke != null && sWidth > 0)
                if (sStyle != null)
                    context.Target.DrawRectangle(rect, stroke, sWidth, sStyle);
                else
                    context.Target.DrawRectangle(rect, stroke, sWidth);
        }

        #region Nested type: Context

        public class Context
        {
            public D2D.Brush BaseFill { get; set; }
            public D2D.Brush BaseStroke { get; set; }
            public D2D.StrokeStyle BaseStrokeStyle { get; set; }
            public float BaseStrokeWidth { get; set; }
            public D2D.RenderTarget Target { get; set; }
        }

        #endregion

        #region Nested type: Format

        public class Format : ComObject
        {
            public D2D.Brush Fill { get; set; }
            public int StrikethroughCount { get; set; }
            public D2D.Brush Stroke { get; set; }
            public D2D.StrokeStyle StrokeStyle { get; set; }
            public float? StrokeWidth { get; set; }
            public int UnderlineCount { get; set; }

            public Format Clone()
            {
                return new Format
                {
                    Fill = Fill,
                    Stroke = Stroke,
                    StrokeWidth = StrokeWidth,
                    StrokeStyle = StrokeStyle,
                    UnderlineCount = UnderlineCount,
                    StrikethroughCount = StrikethroughCount
                };
            }
        }

        #endregion
    }

    public class GeometryTextRenderer : DW.TextRendererBase
    {
        public override Result DrawGlyphRun(object clientDrawingContext, float baselineOriginX, float baselineOriginY,
            D2D.MeasuringMode measuringMode, DW.GlyphRun glyphRun, DW.GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect)
        {
            var context = (Context) clientDrawingContext;
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
                Matrix3x2.Translation(baselineOriginX, baselineOriginY));

            context.Geometries.Add(geometry);

            return Result.Ok;
        }

        #region Nested type: Context

        public class Context
        {
            public D2D.Factory Factory { get; set; }
            public List<D2D.Geometry> Geometries { get; set; } = new List<D2D.Geometry>();
            public List<BrushInfo> Fills { get; set; } = new List<BrushInfo>();
            public List<(BrushInfo, StrokeInfo)> Strokes { get; set; } = new List<(BrushInfo, StrokeInfo)>();
        }

        #endregion 

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
                    Fill = Fill.Clone<BrushInfo>(),
                    Stroke = Stroke.Clone<BrushInfo>(),
                    StrokeInfo = StrokeInfo.Clone<StrokeInfo>(),
                    UnderlineCount = UnderlineCount,
                    StrikethroughCount = StrikethroughCount
                };
            }
        }
    }

    public class Text : Layer, ITextLayer, IMultiGeometricLayer
    {
        private readonly List<Format> _formats = new List<Format>();

        public Text()
        {
            FontWeight = DW.FontWeight.Normal;
            FontStretch = DW.FontStretch.Normal;
            FontStyle = DW.FontStyle.Normal;
            FontSize = 12;
            FontFamilyName = "Arial";
            StrokeInfo = new StrokeInfo();
        }

        public bool IsBlock
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override string ElementName => "text";

        #region IGeometricLayer Members

        public D2D.Geometry[] GetGeometry(ICacheManager cache)
        {
            var layout = cache.GetTextLayout(this);
            var factory = cache.ArtView.Direct2DFactory;
            using (var gtr = new GeometryTextRenderer())
            {
                var ctx = new GeometryTextRenderer.Context
                {
                    Factory = factory
                };

                layout.Draw(ctx, gtr, 0, 0);

                return ctx.Geometries.ToArray();
            }
        }

        public BrushInfo[] GetFills(ICacheManager cache)
        {
            throw new NotImplementedException();
        }

        public (BrushInfo, StrokeInfo)[] GetStrokes(ICacheManager cache)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ITextLayer Members

        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public override string DefaultName => $@"Text ""{Value}""";

        public override RectangleF GetBounds(ICacheManager cache)
        {
            if (IsBlock)
                return new RectangleF(0, 0, Width, Height);

            var metrics = cache.GetTextLayout(this).Metrics;
            return new RectangleF(metrics.Left, metrics.Top, metrics.Width, metrics.Height);
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

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            point = Matrix3x2.TransformPoint(Matrix3x2.Invert(Transform), point);

            cache.GetTextLayout(this)
                .HitTestPoint(point.X, point.Y, out var _, out var isInside);

            return isInside ? this as T : null;
        }

        public override void Render(D2D.RenderTarget target, ICacheManager cache)
        {
            target.Transform = Transform * target.Transform;

            var layout = cache.GetTextLayout(this);
            var stroke = cache.GetStroke(this);
            var context = new TextRenderer.Context
            {
                BaseFill = cache.GetFill(this),
                BaseStroke = stroke.Brush,
                BaseStrokeStyle = stroke.Style,
                BaseStrokeWidth = stroke.Width,
                Target = target
            };

            using (var renderer = new TextRenderer())
            {
                layout.Draw(context, renderer, 0, 0);
            }

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
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

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                RaisePropertyChanged("TextLayout");
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

        public string FontFamilyName
        {
            get => Get<string>();
            set => Set(value);
        }

        public float FontSize
        {
            get => Get<float>();
            set => Set(value);
        }

        public DW.FontStretch FontStretch
        {
            get => Get<DW.FontStretch>();
            set => Set(value);
        }

        public DW.FontStyle FontStyle
        {
            get => Get<DW.FontStyle>();
            set => Set(value);
        }

        public DW.FontWeight FontWeight
        {
            get => Get<DW.FontWeight>();
            set => Set(value);
        }

        public DW.TextLayout GetLayout(DW.Factory dwFactory)
        {
            var layout = new DW.TextLayout(
                dwFactory,
                Value,
                new DW.TextFormat(
                    dwFactory,
                    FontFamilyName,
                    FontWeight,
                    FontStyle,
                    FontStretch,
                    FontSize * 96 / 72),
                IsBlock ? Width : float.PositiveInfinity,
                IsBlock ? Height : float.PositiveInfinity);

            foreach (var format in _formats)
            {
                //var typography = new DW.Typography(dwFactory);

                //if (format.Superscript)
                //    typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Superscript, 0));

                //if (format.Subscript)
                //    typography.AddFontFeature(new DW.FontFeature(DW.FontFeatureTag.Subscript, 0));

                //layout.SetTypography(typography, format.Range);

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
            }

            return layout;
        }

        public void Insert(int position, string str)
        {
            // expand the length of the format
            var format = GetFormat(position, out var index);

            if (format != null)
            {
                format.Range = new DW.TextRange(
                    format.Range.StartPosition,
                    format.Range.Length + str.Length);
            }

            index++;

            // offset all of the formats that come after this
            while (index < _formats.Count)
            {
                format = _formats[index];
                format.Range = new DW.TextRange(
                    format.Range.StartPosition + str.Length,
                    format.Range.Length);
                index++;
            }

            Value = Value.Insert(position, str);
        }

        public void Remove(int position, int length)
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

                if (len <= 0 && fend <= end) _formats.Remove(format);
                else if (len <= 0 && fend > end) format.Range = new DW.TextRange(fstart, fend - fstart- length);
                else format.Range = new DW.TextRange(fstart, len);

                current++;
            }
            
            index++;

            // offset all of the formats that come after this
            while (index < _formats.Count)
            {
                var format = _formats[index];
                format.Range = new DW.TextRange(
                    format.Range.StartPosition - length,
                    format.Range.Length);
                index++;
            }

            Value = Value.Remove(position, length);
        }

        public string Value
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        public Format GetFormat(int position)
        {
            return GetFormat(position, out var _);
        }

        public Format GetFormat(int position, out int index)
        {
            var i = 0;

            do i++; while (i < _formats.Count && _formats[i].Range.StartPosition <= position);

            var format = _formats.ElementAtOrDefault(--i);
            index = i;
            return format?.Range.Length > position - format?.Range.StartPosition ? format : null;
        }

        public void SetFormat(Format format)
        {
            var range = format.Range;
            var start = range.StartPosition;
            var current = range.StartPosition;
            var end = range.StartPosition + range.Length;

            if (start == end) return;

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

                    _formats.Remove(oldFormat);
                    _formats.Add(newFormat);

                    if (nStart > oStart)
                    {
                        var iFormat = oldFormat.Clone();
                        iFormat.Range = new DW.TextRange(oStart, nStart - oStart);
                        _formats.Add(iFormat);
                    }

                    if (nEnd < oEnd)
                    {
                        var iFormat = oldFormat.Clone();
                        iFormat.Range = new DW.TextRange(nEnd, oEnd - nEnd);
                        _formats.Add(iFormat);
                    }

                    if (start < Math.Min(nStart, oStart))
                    {
                        var iFormat = format.Clone();
                        iFormat.Range = new DW.TextRange(start, Math.Min(nStart, oStart) - start);
                        _formats.Add(iFormat);
                    }

                    start = Math.Max(nEnd, oEnd);

                    current += oLen;
                }
                else current++;
            }

            if (start < end)
                _formats.Add(format);

            _formats.Sort((f1, f2) => f1.Range.StartPosition.CompareTo(f2.Range.StartPosition));

            Trace.WriteLine(
                "processing complete:\n" +
                string.Join("\n",
                    _formats.Select(f => $"{f.Range.StartPosition} + {f.Range.Length} -> " +
                                         $"{f.Range.StartPosition + f.Range.Length}: " +
                                         $"{f.FontWeight.GetValueOrDefault(DW.FontWeight.Normal)} " +
                                         $"{f.FontStyle.GetValueOrDefault()}")));

            RaisePropertyChanged("TextLayout");
        }

        #region Nested type: Format

        public sealed class Format
        {
            public Format Union(Format f)
            {
                return new Format
                {
                    FontFamilyName = f.FontFamilyName ?? FontFamilyName,
                    FontStyle = f.FontStyle ?? FontStyle,
                    FontSize = f.FontSize ?? FontSize,
                    FontStretch = f.FontStretch ?? FontStretch,
                    FontWeight = f.FontWeight ?? FontWeight,
                    Subscript = f.Subscript || Subscript,
                    Superscript = f.Superscript || Superscript
                };
            }

            private bool _subscript;
            private bool _superscript;

            public string FontFamilyName { get; set; }

            public float? FontSize { get; set; }

            public DW.FontStretch? FontStretch { get; set; }

            public DW.FontStyle? FontStyle { get; set; }

            public DW.FontWeight? FontWeight { get; set; }

            public DW.TextRange Range { get; set; }

            public bool Subscript
            {
                get => _subscript;
                set
                {
                    _subscript = value;
                    if (value) Superscript = false;
                }
            }

            public bool Superscript
            {
                get => _superscript;
                set
                {
                    _superscript = value;
                    if (value) Subscript = false;
                }
            }

            public Format Clone()
            {
                return new Format
                {
                    Superscript = Superscript,
                    Subscript = Subscript,
                    FontSize = FontSize,
                    FontFamilyName = FontFamilyName,
                    FontStyle = FontStyle,
                    FontStretch = FontStretch,
                    FontWeight = FontWeight
                };
            }
        }

        #endregion
    }
}