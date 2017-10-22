using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public abstract class BrushInfo : Resource
    {
        private static long _nextId = 1;

        protected BrushInfo()
        {
            Opacity = 1;
            Transform = Matrix3x2.Identity;
            Name = _nextId++.ToString();
        }

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

        public abstract IBrush CreateBrush(RenderContext target);
    }
}