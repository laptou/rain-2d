using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

using SharpDX.WIC;

namespace Rain.Renderer.WIC {
    public class ImageFrame : IImageFrame
    {
        private readonly SharpDX.WIC.Bitmap _img;

        public ImageFrame(ImagingFactory fac, IImage image, BitmapSource img)
        {
            Factory = fac;
            _img = new SharpDX.WIC.Bitmap(fac, img, BitmapCreateCacheOption.CacheOnDemand);
            Image = image;
            Width = img.Size.Width;
            Height = img.Size.Height;
        }

        internal ImagingFactory Factory { get; }


        internal SharpDX.WIC.Bitmap GetWicBitmap() { return _img; }

        #region IImageFrame Members

        /// <inheritdoc />
        public void Dispose() { _img?.Dispose(); }

        /// <inheritdoc />
        public IImageLock GetReadLock()
        {
            return new ImageFrameLock(this, GetWicBitmap(), BitmapLockFlags.Read);
        }

        /// <inheritdoc />
        public IImageLock GetWriteLock()
        {
            return new ImageFrameLock(this, GetWicBitmap(), BitmapLockFlags.Write);
        }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public IImage Image { get; }

        /// <inheritdoc />
        public int Width { get; }

        #endregion
    }
}