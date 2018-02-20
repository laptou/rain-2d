using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public class SolidColorBrushInfo : BrushInfo
    {
        public SolidColorBrushInfo() { }

        public SolidColorBrushInfo(Color color) { Color = color; }

        public Color Color
        {
            get => Get<Color>();
            set => Set(value);
        }


        public override IBrush CreateBrush(RenderContext target)
        {
            return target.CreateBrush(Color);
        }

        public override string ToString() { return $"Color: {Color}, Opacity: {Opacity}"; }
    }
}