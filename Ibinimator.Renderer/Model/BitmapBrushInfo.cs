using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using SharpDX.Direct2D1;

namespace Ibinimator.Renderer.Model
{
    public class BitmapBrushInfo : BrushInfo
    {
        public byte[] Bitmap { get; set; }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
            set => Set(value);
        }

        public override IBrush CreateBrush(RenderContext target) { throw new NotImplementedException(); }
    }
}