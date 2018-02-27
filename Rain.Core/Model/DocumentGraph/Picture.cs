using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;

using Rain.Core.Model.Imaging;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Picture : Layer, IImageLayer
    {
        public IImage Image
        {
            get => Get<IImage>();
            set => Set(value, RaiseImageChanged);
        }

        public int Frame
        {
            get => Get<int>();
            set => Set(value, RaiseImageChanged);
        }

        private void RaiseImageChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseImageChanged();
        }

        private void RaiseImageChanged()
        {
            ImageChanged?.Invoke(this, null);
        }

        /// <inheritdoc />
        public override RectangleF GetBounds(IArtContext ctx)
        {
            return new RectangleF(0, 0, Width, Height);
        }

        /// <inheritdoc />
        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            if (!(this is T t)) return default;
            if (minimumDepth > 0) return default;

            var pt = Vector2.Transform(point, MathUtils.Invert(AbsoluteTransform));

            var bounds = cache.GetBounds(this);

            return bounds.Contains(pt) ? t : default;
        }

        /// <inheritdoc />
        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {

            if (!Visible) return;

            // grabbing the value here avoids jitter if the transform is changed during the rendering
            var transform = Transform;

            var image = cache.GetImage(this);

            target.Transform(transform);

            var rect = new RectangleF(0, 0, Width, Height);

            target.DrawBitmap(image, rect);

            target.Transform(MathUtils.Invert(transform));
        }

        public virtual float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

        /// <inheritdoc />
        public event EventHandler ImageChanged;

        /// <inheritdoc />
        public IRenderImage GetImage(IArtContext ctx)
        {
            return ctx.RenderContext.GetRenderImage(Image.Frames[Frame]);
        }
    }
}
