using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;
using Rain.Renderer.WIC;

using SharpDX.DXGI;
using SharpDX.WIC;

using D2D1 = SharpDX.Direct2D1;
using DX = SharpDX;

namespace Rain.Renderer.Direct2D
{
    internal class Bitmap : ResourceBase, IRenderImage
    {
        private readonly D2D1.Bitmap _bmp;

        public Bitmap(D2D1.RenderTarget ctx, ImageFrame image)
        {
            var wicBmp = image.GetWicBitmap();
            BitmapSource wicSrc = wicBmp;

            if (wicBmp.PixelFormat != PixelFormat.Format32bppPBGRA)
            {
                var converter = new FormatConverter(image.Factory);
                converter.Initialize(wicSrc, PixelFormat.Format32bppPBGRA);
                wicSrc = converter;
            }

            var fmt = new D2D1.PixelFormat(Format.B8G8R8A8_UNorm, D2D1.AlphaMode.Premultiplied);
            wicBmp.GetResolution(out var dpix, out var dpiy);
            var props = new D2D1.BitmapProperties(fmt, (float) dpix, (float) dpiy);
            _bmp = D2D1.Bitmap.FromWicBitmap(ctx, wicSrc, props);

            Alpha = image.Image.Alpha;
        }

        public Bitmap(D2D1.Bitmap bitmap) { _bmp = bitmap; }

        public static implicit operator D2D1.Bitmap(Bitmap bmp) { return bmp._bmp; }

        #region IRenderImage Members

        public override void Dispose()
        {
            _bmp.Dispose();
            base.Dispose();
        }

        public T Unwrap<T>() where T : class { return _bmp as T; }

        public override void Optimize()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Alpha { get; }

        public float Dpi => _bmp.DotsPerInch.Width;

        public float Height => _bmp.Size.Height;

        public int PixelHeight => _bmp.PixelSize.Height;

        public int PixelWidth => _bmp.PixelSize.Width;

        public float Width => _bmp.Size.Width;

        #endregion
    }
}