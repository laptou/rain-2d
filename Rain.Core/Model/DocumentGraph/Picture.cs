using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;

namespace Rain.Core.Model.DocumentGraph
{
    public class Picture : Layer, IImageLayer
    {
        public int Frame
        {
            get => Get<int>();
            set => Set(value, RaiseImageChanged);
        }

        public IImage Image
        {
            get => Get<IImage>();
            set => Set(value, RaiseImageChanged);
        }

        private void RaiseImageChanged(object sender, PropertyChangedEventArgs e) { RaiseImageChanged(); }

        private void RaiseImageChanged() { ImageChanged?.Invoke(this, null); }

        #region IImageLayer Members

        /// <inheritdoc />
        public event EventHandler ImageChanged;

        /// <inheritdoc />
        public override RectangleF GetBounds(IArtContext ctx)
        {
            return new RectangleF(0, 0, Image.Frames[0].Width, Image.Frames[0].Height);
        }

        /// <inheritdoc />
        public IRenderImage GetImage(IArtContext ctx) { return ctx.RenderContext.GetRenderImage(Image.Frames[Frame]); }

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
        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            // grabbing the value here avoids jitter if the transform is changed during the rendering
            var transform = Transform;

            var image = cache.GetImage(this);

            target.Transform(transform);

            var factor = transform.GetScale();
            var mode = factor.Length() < MathUtils.Sqrt2 ? ScaleMode.HighQualityCubic : ScaleMode.MultiSampleLinear;


            target.DrawBitmap(image, new RectangleF(0, 0, Image.Frames[Frame].Width, Image.Frames[Frame].Height), mode);

            target.Transform(MathUtils.Invert(transform));
        }

        /// <inheritdoc />
        public override string DefaultName => $"Image, {Image.Frames[0].Width}×{Image.Frames[0].Height}";

        #endregion
    }
}