using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;

namespace Ibinimator.Renderer.Model
{
    public class PenInfo : Core.Model.Model, IPenInfo
    {
        public PenInfo() { Dashes = new ObservableList<float>(); }

        public IBrushInfo Brush
        {
            get => Get<IBrushInfo>();
            set => Set(value);
        }

        public ObservableList<float> Dashes
        {
            get => Get<ObservableList<float>>();
            set => Set(value);
        }

        public float DashOffset
        {
            get => Get<float>();
            set => Set(value);
        }

        public LineCap LineCap
        {
            get => Get<LineCap>();
            set => Set(value);
        }

        public LineJoin LineJoin
        {
            get => Get<LineJoin>();
            set => Set(value);
        }

        public bool HasDashes
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        public float MiterLimit
        {
            get => Get<float>();
            set => Set(value);
        }

        public IPen CreatePen(RenderContext renderCtx)
        {
            return renderCtx.CreatePen(Width,
                                       Brush?.CreateBrush(renderCtx),
                                       Dashes,
                                       DashOffset,
                                       LineCap,
                                       LineJoin,
                                       MiterLimit);
        }
    }
}