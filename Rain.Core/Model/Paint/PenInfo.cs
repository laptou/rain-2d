using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Utility;

namespace Rain.Core.Model.Paint
{
    public class PenInfo : ResourceBase, IPenInfo
    {
        public PenInfo()
        {
            Dashes = new ObservableList<float>();
            MiterLimit = 4f;
        }

        public PenInfo(float width) : this() { Width = width; }
        public PenInfo(float width, IBrushInfo brush) : this(width) { Brush = brush; }

        #region IPenInfo Members

        public IPen CreatePen(IRenderContext renderCtx)
        {
            return renderCtx.CreatePen(Width,
                                       Brush?.CreateBrush(renderCtx),
                                       Dashes,
                                       DashOffset,
                                       LineCap,
                                       LineJoin,
                                       MiterLimit);
        }

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

        public bool HasDashes
        {
            get => Get<bool>();
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

        public float MiterLimit
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        #endregion

        /// <inheritdoc />
        public override bool Optimized => false;
    }
}