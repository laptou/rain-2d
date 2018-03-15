using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace Rain.Renderer.Direct2D
{
    internal class TextRenderer : TextRendererBase
    {
        public override Result DrawGlyphRun(
            object clientDrawingContext, float baselineOriginX, float baselineOriginY,
            MeasuringMode measuringMode, GlyphRun glyphRun, GlyphRunDescription glyphRunDescription,
            ComObject clientDrawingEffect)
        {
            var context = (Context) clientDrawingContext;
            var format = (Format) clientDrawingEffect;

            var path = new PathGeometry(context.RenderContext.FactoryD2D);
            var sink = path.Open();

            glyphRun.FontFace.GetGlyphRunOutline(glyphRun.FontSize,
                                                 glyphRun.Indices,
                                                 glyphRun.Advances,
                                                 glyphRun.Offsets,
                                                 glyphRun.IsSideways,
                                                 glyphRun.BidiLevel % 2 != 0,
                                                 sink);

            sink.Close();

            var geometry = new TransformedGeometry(context.RenderContext.FactoryD2D,
                                                   path,
                                                   Matrix3x2.Translation(
                                                       baselineOriginX,
                                                       baselineOriginY));

            context.Geometries.Add(new Geometry(context.RenderContext.Target, geometry));
            context.Brushes.Add(format?.Fill?.CreateBrush(context.RenderContext));
            context.Pens.Add(format?.Stroke?.CreatePen(context.RenderContext));

            context.GlyphCount += glyphRun.Indices.Length;
            context.CharactersForGeometry.Add(context.GeometryCount,
                                              glyphRunDescription.Text.Length);
            context.GeometryCount++;

            return Result.Ok;
        }

        #region Nested type: Context

        public class Context : IDisposable
        {
            public Context(Direct2DRenderContext ctx) { RenderContext = ctx; }

            public List<IBrush> Brushes { get; } = new List<IBrush>();
            public Dictionary<int, int> CharactersForGeometry { get; } = new Dictionary<int, int>();
            public List<IGeometry> Geometries { get; } = new List<IGeometry>();
            public int GeometryCount { get; set; }
            public int GlyphCount { get; set; }
            public List<IPen> Pens { get; } = new List<IPen>();

            public Direct2DRenderContext RenderContext { get; }

            public void Dispose()
            {
                foreach (var brush in Brushes)
                    brush?.Dispose();

                foreach (var geometry in Geometries)
                    geometry?.Dispose();

                foreach (var pen in Pens)
                    pen?.Dispose();

                Brushes.Clear();
                Geometries.Clear();
                Pens.Clear();
                CharactersForGeometry.Clear();
                GeometryCount = GlyphCount = 0;

            }
        }

        #endregion

        #region Nested type: Format

        public class Format : ComObject
        {
            public BrushInfo Fill { get; set; }
            public int StrikethroughCount { get; set; }
            public PenInfo Stroke { get; set; }
            public int UnderlineCount { get; set; }

            public Format Clone()
            {
                return new Format
                {
                    Fill = (BrushInfo) Fill?.Clone(),
                    Stroke = (PenInfo) Stroke?.Clone(),
                    UnderlineCount = UnderlineCount,
                    StrikethroughCount = StrikethroughCount
                };
            }
        }

        #endregion

        //public override DX.Result DrawStrikethrough(object clientDrawingContext, float baselineOriginX,
        //    float baselineOriginY, ref DW.Strikethrough strikethrough, 
        //    DX.ComObject clientDrawingEffect)
        //{
        //    DrawLine(
        //        clientDrawingContext as Context,
        //        clientDrawingEffect as Format,
        //        baselineOriginX,
        //        baselineOriginY + strikethrough.Offset,
        //        strikethrough.Width,
        //        strikethrough.Thickness);

        //    return Result.Ok;
        //}

        //public override DX.Result DrawUnderline(object clientDrawingContext, float baselineOriginX,
        //    float baselineOriginY, ref DW.Underline underline,
        //    DX.ComObject clientDrawingEffect)
        //{
        //    DrawLine(
        //        clientDrawingContext as Context,
        //        clientDrawingEffect as Format,
        //        baselineOriginX,
        //        baselineOriginY + underline.Offset,
        //        underline.Width,
        //        underline.Thickness);

        //    return DX.Result.Ok;
        //}

        //private static void DrawLine(Context context, Format format, float x, float y, float width, float thickness)
        //{
        //    var rect = new D2D.RectangleGeometry(
        //        context.Factory, 
        //        new RectangleF(x, y, width, thickness));

        //    context.Geometries.Add(new Geometry(context.RenderContext.Target, rect));
        //}
    }
}