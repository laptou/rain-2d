using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Reactive.Linq;
using System.Text;

using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Picture : Layer, IImageLayer
    {
        private Vector2 _resolution;

        public Picture()
        {
            // adjust image sampling every 1000ms
            var transformObservable = Observable
                                     .FromEventPattern<PropertyChangedEventHandler,
                                          PropertyChangedEventArgs>(
                                          h => PropertyChanged += h,
                                          h => PropertyChanged -= h)
                                     .Where(evt => evt.EventArgs.PropertyName == nameof(Transform))
                                     .Throttle(TimeSpan.FromMilliseconds(250));
            transformObservable.Subscribe(evt => UpdateResolution());
        }

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

        private void RaiseImageChanged() { ImageChanged?.Invoke(this, null); }

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

            target.DrawBitmap(image, new RectangleF(0, 0, Width, Height));

            target.Transform(MathUtils.Invert(transform));
        }

        public virtual float Width
        {
            get => Get<float>();
            set => Set(value, SizeChanged);
        }

        private void SizeChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateResolution();
            RaiseBoundsChanged();
        }

        private void UpdateResolution()
        {
            var d = AbsoluteTransform.Decompose();
            var dim = new Vector2(Width, Height);
            var res = MathUtils.Abs(Vector2.Transform(dim, d.scale));
            var diff = res / _resolution;

            if (_resolution == Vector2.Zero || Vector2.Distance(diff, Vector2.One) >= 0.1f)
            {
                _resolution = res;
                // if there is more than a 10% difference in size, regenerate the image
                RaiseImageChanged();
            }
        }

        public virtual float Height
        {
            get => Get<float>();
            set => Set(value, SizeChanged);
        }

        /// <inheritdoc />
        public event EventHandler ImageChanged;

        /// <inheritdoc />
        public IRenderImage GetImage(IArtContext ctx)
        {
            var image = Image.Frames[Frame];
            var factor = new Vector2(_resolution.X / image.Width, _resolution.Y / image.Height);
            var mode = factor.Length() < 1
                           ? ScaleMode.HighQualityCubic
                           : ScaleMode.MultiSampleLinear;

            return ctx.RenderContext.GetRenderImage(image, factor, mode);
        }
    }
}