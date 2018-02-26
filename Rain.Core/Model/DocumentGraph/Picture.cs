using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;

using Rain.Core.Model.Imaging;

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
        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public override void Render(RenderContext target, ICacheManager cache, IViewManager view) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public event EventHandler ImageChanged;

        /// <inheritdoc />
        public IRenderImage GetImage(ICacheManager cache) { return Image.Frames[Frame].GetRenderImage(); }
    }
}
