using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

using SharpDX.IO;
using SharpDX.WIC;

using D2D1 = SharpDX.Direct2D1;

namespace Rain.Renderer.Direct2D
{
    internal class Image : IImage
    {
        private readonly D2D1.RenderTarget _target;
        private WICStream _stream;

        public Image(Direct2DRenderContext ctx)
        {
            _target = ctx.Target;
            Factory = new ImagingFactory();
        }

        internal ImagingFactory Factory { get; }

        internal void Load(Stream stream)
        {
            _stream?.Dispose();
            _stream = new WICStream(Factory, stream);
            using (var decoder = new BitmapDecoder(Factory, _stream, DecodeOptions.CacheOnLoad))
                Decode(decoder);
        }

        internal void Load(string fn)
        {
            using (var decoder =
                new BitmapDecoder(Factory, fn, NativeFileAccess.Read, DecodeOptions.CacheOnLoad))
                Decode(decoder);
        }

        private void Decode(BitmapDecoder decoder)
        {
            var frames = new List<IImageFrame>();

            for (var i = 0; i < decoder.FrameCount; i++)
            {
                var frame = decoder.GetFrame(i);
                var metadataReader = frame.MetadataQueryReader;

                if (metadataReader != null)
                    using (metadataReader)
                    {
                        foreach (var path in metadataReader.QueryPaths)
                        {
                            var result =
                                metadataReader.TryGetMetadataByName(path, out var metadata);
                        }
                    }

                frames.Add(new ImageFrame(Factory, this, frame));
            }

            Frames = frames;
        }

        #region IImage Members

        /// <inheritdoc />
        public void Dispose()
        {
            _stream?.Dispose();
            Factory?.Dispose();

            foreach (var frame in Frames)
                frame?.Dispose();
        }

        /// <inheritdoc />
        public bool Alpha { get; set; }

        /// <inheritdoc />
        public ColorFormat ColorFormat { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IImageFrame> Frames { get; private set; }

        /// <inheritdoc />
        public ImageFormat ImageFormat { get; set; }

        #endregion
    }

    internal class ImageFrame : IImageFrame
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
        public IRenderImage GetRenderImage() { throw new NotImplementedException(); }

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

    internal class ImageFrameLock : IImageLock
    {
        private readonly BitmapLock _lock;

        public ImageFrameLock(IImageFrame imageFrame, SharpDX.WIC.Bitmap bmp, BitmapLockFlags flags)
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