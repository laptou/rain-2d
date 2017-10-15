using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using SharpDX.Direct2D1;

namespace Ibinimator.Renderer.Model
{
    public class PenInfo : Model
    {
        public PenInfo()
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

        public BrushInfo Brush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public IPen CreatePen(RenderContext renderCtx)
        {
            return renderCtx.CreatePen(Width, Brush.CreateBrush(renderCtx), Dashes);
        }
    }
}