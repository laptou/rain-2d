using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public class SolidColorBrushInfo : BrushInfo, ISolidColorBrushInfo
    {
        public SolidColorBrushInfo() { }

        public SolidColorBrushInfo(Color color) { Color = color; }

        public SolidColorBrushInfo(float r, float g, float b) : this(new Color(r, g, b)) { }

        public SolidColorBrushInfo(float r, float g, float b, float a) : this(new Color(r, g, b, a)) { }

        public override string ToString() { return $"Color: {Color}, Opacity: {Opacity}"; }

        #region ISolidColorBrushInfo Members

        public override IBrush CreateBrush(IRenderContext target) { return target.CreateBrush(Color); }

        public Color Color
        {
            get => Get<Color>();
            set => Set(value);
        }

        #endregion
    }
}