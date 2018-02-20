using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public class BitmapBrushInfo : BrushInfo
    {
        public byte[] Bitmap { get; set; }

        public SpreadMethod SpreadMethod
        {
            get => Get<SpreadMethod>();
            set => Set(value);
        }

        public override IBrush CreateBrush(RenderContext target)
        {
            throw new NotImplementedException();
        }
    }
}