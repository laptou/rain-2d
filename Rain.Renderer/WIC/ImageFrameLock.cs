using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

using SharpDX.WIC;

namespace Rain.Renderer.WIC
{
    public class ImageFrameLock : IImageLock
    {
        private readonly BitmapLock _lock;

        public ImageFrameLock(IImageFrame imageFrame, Bitmap bmp, BitmapLockFlags flags)
        {
            ImageFrame = imageFrame;


            _lock = bmp.Lock(flags);
        }

        #region IImageLock Members

        /// <inheritdoc />
        public void Dispose() { _lock?.Dispose(); }

        /// <inheritdoc />
        public T[] GetPixels<T>(int count) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public void SetPixels<T>(T[] data) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public IImageFrame ImageFrame { get; }

        #endregion
    }
}