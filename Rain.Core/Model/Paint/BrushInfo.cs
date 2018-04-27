using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public abstract class BrushInfo : ResourceBase, IBrushInfo
    {
        private static long _nextId = 1;

        protected BrushInfo()
        {
            Opacity = 1;
            Transform = Matrix3x2.Identity;
            Name = _nextId++.ToString();
        }

        /// <param name="context"></param>
        /// <inheritdoc />
        public override void Optimize(IRenderContext context) { throw new NotImplementedException(); }

        #region IBrushInfo Members

        public abstract IBrush CreateBrush(IRenderContext target);

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            set => Set(value);
        }

        #endregion
    }
}