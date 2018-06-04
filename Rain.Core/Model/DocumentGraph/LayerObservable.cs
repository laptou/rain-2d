using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;


namespace Rain.Core.Model.DocumentGraph
{
    public static class LayerObservable
    {
        public static IObservable<RectangleF> CreateBoundsObservable(this ILayer layer, IArtContext ctx)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.BoundsChanged += a, r => layer.BoundsChanged -= r)
                  .Select(args => layer.GetBounds(ctx));
        }

        public static IObservable<IGeometricLayer> CreateClipObservable(this ILayer layer)
        {
            return layer.CreateObservable(l => l.Clip);
        }

        public static IObservable<IBrushInfo> CreateFillObservable(this IFilledLayer layer)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.FillChanged += a, r => layer.FillChanged -= r)
                  .Select(args => layer.Fill);
        }

        public static IObservable<IGeometry> CreateGeometryObservable(this IGeometricLayer layer, IArtContext ctx)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.GeometryChanged += a, r => layer.GeometryChanged -= r)
                  .Select(args => layer.GetGeometry(ctx));
        }

        public static IObservable<IRenderImage> CreateImageObservable(this IImageLayer layer, IArtContext ctx)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.ImageChanged += a, r => layer.ImageChanged -= r)
                  .Select(args => layer.GetImage(ctx));
        }

        public static IObservable<bool> CreateSelectedObservable(this ILayer layer)
        {
            return layer.CreateObservable(l => l.Selected);
        }

        public static IObservable<IPenInfo> CreateStrokeObservable(this IStrokedLayer layer)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.StrokeChanged += a, r => layer.StrokeChanged -= r)
                  .Select(args => layer.Stroke);
        }

        public static IObservable<ITextLayout> CreateTextLayoutObservable(this ITextLayer layer, IArtContext ctx)
        {
            return Observable
                  .FromEvent<EventHandler, EventArgs>(a => layer.LayoutChanged += a, r => layer.LayoutChanged -= r)
                  .Select(args => layer.GetLayout(ctx));
        }

        public static IObservable<Matrix3x2> CreateTransformObservable(this ILayer layer)
        {
            return layer.CreateObservable(l => l.Transform);
        }
    }
}