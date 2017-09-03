using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        public class Context
        {
            public D2D.RenderTarget Target { get; set; }
            public D2D.Brush BaseFill { get; set; }
            public D2D.Brush BaseStroke { get; set; }
            public float BaseStrokeWidth { get; set; }
            public D2D.StrokeStyle1 BaseStrokeStyle { get; set; }
        }

        public class Format : ComObject
        {
            public D2D.Brush Fill { get; set; }
            public D2D.Brush Stroke { get; set; }
            public float? StrokeWidth { get; set; }
            public D2D.StrokeStyle1 StrokeStyle { get; set; }
            public int UnderlineCount { get; set; }
            public int StrikethroughCount { get; set; }

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

        public override RawMatrix3x2 GetCurrentTransform(object clientDrawingContext)
        {
            var context = (Context) clientDrawingContext;
            return context.Target.Transform;
        }

        public override float GetPixelsPerDip(object clientDrawingContext)
        {
            var context = (Context)clientDrawingContext;
            return context.Target.DotsPerInch.Height / 96f;
        }

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
                glyphRun.BidiLevel % 2 == 0, 
                sink);

            sink.Close();

            var fill = format.Fill ?? context.BaseFill;

            if(fill != null)
                context.Target.FillGeometry(path, fill);

            var stroke = format.Stroke ?? context.BaseStroke;
            var width = format.StrokeWidth ?? context.BaseStrokeWidth;
            var style = format.StrokeStyle ?? context.BaseStrokeStyle;

            if(stroke != null && width > 0)
                if(style != null)
                    context.Target.DrawGeometry(path, stroke, width, style);
                else
                    context.Target.DrawGeometry(path, stroke, width);

            return Result.Ok;
        }

        public override Result DrawUnderline(object clientDrawingContext, float baselineOriginX, float baselineOriginY, ref DW.Underline underline,
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

        public override Result DrawStrikethrough(object clientDrawingContext, float baselineOriginX, float baselineOriginY,
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
    }

    public interface ITextLayer : IFilledLayer, IStrokedLayer
    {
        string Value { get; set; }
        float FontSize { get; set; }
        string FontFamilyName { get; set; }
        DW.FontWeight FontWeight { get; set; }
        DW.FontStretch FontStretch { get; set; }
        DW.FontStyle FontStyle { get; set; }
        DW.TextLayout GetLayout(DW.Factory dwFactory);
    }

    public class Text : Layer, ITextLayer
    {
        protected override string ElementName => "text";

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            throw new NotImplementedException();
            
        }

        public override void Render(D2D.RenderTarget target, ICacheManager cache)
        {
            target.Transform = Transform * target.Transform;

            var factory = cache.ArtView.DirectWriteFactory;
            var layout = cache.GetTextLayout(this);
            var context = new TextRenderer.Context {BaseFill = cache.GetFill(this)};

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
        }

        public string Value
        {
            get => Get<string>();
            set => Set(value);
        }

        public float FontSize
        {
            get => Get<float>();
            set => Set(value);
        }

        public string FontFamilyName
        {
            get => Get<string>();
            set => Set(value);
        }

        public DW.FontWeight FontWeight
        {
            get => Get<DW.FontWeight>();
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
                Width, 
                Height);

            layout.SetFontWeight(DW.FontWeight.Bold, new DW.TextRange(0, 1));

            return layout;
        }

        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
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
