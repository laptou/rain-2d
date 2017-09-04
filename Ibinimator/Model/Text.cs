using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            StrokeDashes = new ObservableCollection<float>(new float[] { 0, 0, 0, 0 });
            StrokeStyle = new D2D.StrokeStyleProperties1
            {
                TransformType = D2D.StrokeTransformType.Fixed
            };
        }

        public bool IsBlock
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override string ElementName => "text";

        #region IGeometricLayer Members

        public D2D.Geometry GetGeometry(ICacheManager cache)
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

                return new D2D.GeometryGroup(
                    factory,
                    D2D.FillMode.Winding,
                    ctx.Geometries.ToArray());
            }
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
                BaseStroke = stroke.brush,
                BaseStrokeStyle = stroke.style,
                BaseStrokeWidth = stroke.width,
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

        public ObservableCollection<float> StrokeDashes
        {
            get => Get<ObservableCollection<float>>();
            set => Set(value);
        }

        public D2D.StrokeStyleProperties1 StrokeStyle
        {
            get => Get<D2D.StrokeStyleProperties1>();
            set => Set(value);
        }

        public float StrokeWidth
        {
            get => Get<float>();
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
                    FontSize),
                IsBlock ? Width : float.PositiveInfinity,
                IsBlock ? Height : float.PositiveInfinity);

            layout.SetFontWeight(DW.FontWeight.Bold, new DW.TextRange(0, 1));

            return layout;
        }

        public string Value
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        private static int[] ToCodePoints(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            var codePoints = new List<int>(str.Length);

            for (var i = 0; i < str.Length; i++)
            {
                codePoints.Add(char.ConvertToUtf32(str, i));

                if (char.IsHighSurrogate(str[i]))
                    i++;
            }

            return codePoints.ToArray();
        }
    }
}