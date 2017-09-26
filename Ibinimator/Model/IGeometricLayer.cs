using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Service;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    public interface IFilledLayer : ILayer
    {
        BrushInfo FillBrush { get; set; }
    }

    public interface IStrokedLayer : ILayer
    {
        BrushInfo StrokeBrush { get; set; }

        StrokeInfo StrokeInfo { get; set; }
    }

    public class StrokeInfo : Model
    {
        public StrokeInfo()
        {
            Dashes = new ObservableList<float>();
            Style = new StrokeStyleProperties1();
        }

        public ObservableList<float> Dashes
        {
            get => Get<ObservableList<float>>();
            set => Set(value);
        }

        public StrokeStyleProperties1 Style
        {
            get => Get<StrokeStyleProperties1>();
            set => Set(value);
        }

        public float Width
        {
            get => Get<float>();
            set => Set(value);
        }
    }

    public class Stroke : DisposeBase
    {
        public Stroke(RenderTarget target, BrushInfo stroke, StrokeInfo strokeInfo)
        {
            Brush = stroke?.ToDirectX(target);
            Style = new StrokeStyle1(
                target.Factory.QueryInterface<Factory1>(),
                strokeInfo?.Style ?? default(StrokeStyleProperties1),
                strokeInfo?.Dashes.ToArray() ?? new float[0]);
            Width = strokeInfo?.Width ?? 0;
        }

        public Stroke()
        {
        }

        public Brush Brush { get; set; }

        public StrokeStyle1 Style { get; set; }

        public float Width { get; set; }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                Brush?.Dispose();
                Style?.Dispose();
            }
        }
    }

    public interface IGeometricLayer : IFilledLayer, IStrokedLayer
    {
        Geometry GetGeometry(ICacheManager cache);
    }

    public class Figure : IDisposable
    {
        public BrushInfo FillBrush { get; set; }
        public Geometry Geometry { get; set; }

        public BrushInfo StrokeBrush { get; set; }

        public StrokeInfo StrokeInfo { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            Geometry?.Dispose();
        }

        #endregion
    }
}