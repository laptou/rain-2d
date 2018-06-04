using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

using SharpDX.IO;
using SharpDX.WIC;

namespace Rain.Renderer.WIC
{
    public class Image : IImage
    {
        private WICStream _stream;

        public Image() { Factory = new ImagingFactory(); }

        internal ImagingFactory Factory { get; }

        internal void Load(Stream stream)
        {
            _stream?.Dispose();
            _stream = new WICStream(Factory, stream);

            using (var decoder = new BitmapDecoder(Factory, _stream, DecodeOptions.CacheOnLoad))
            {
                Decode(decoder);
            }
        }

        internal void Load(string fn)
        {
            using (var decoder = new BitmapDecoder(Factory, fn, NativeFileAccess.Read, DecodeOptions.CacheOnLoad))
            {
                Decode(decoder);
            }
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
                            var result = metadataReader.TryGetMetadataByName(path, out var metadata);
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
}