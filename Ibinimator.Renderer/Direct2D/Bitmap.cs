using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using D2D1 = SharpDX.Direct2D1;
using DX = SharpDX;

namespace Ibinimator.Renderer.Direct2D
{
    internal class Bitmap : ResourceBase, IBitmap
    {
        private readonly D2D1.Bitmap _bmp;

        public Bitmap(Direct2DRenderContext ctx, Stream stream)
        {
            using (var bitmap = (System.Drawing.Bitmap) Image.FromStream(stream))
            {
                var sourceArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                var bitmapProperties = new D2D1.BitmapProperties(
                    new D2D1.PixelFormat(
                        SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        D2D1.AlphaMode.Premultiplied),
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution);

                var data = bitmap.LockBits(sourceArea,
                                           ImageLockMode.ReadOnly,
                                           PixelFormat.Format32bppPArgb);

                using (var temp =
                    new DX.DataStream(
                        data.Scan0,
                        bitmap.Width * sizeof(int),
                        true,
                        true))
                {
                    var bmp = new D2D1.Bitmap(
                        ctx.Target,
                        new DX.Size2(sourceArea.Width, sourceArea.Height),
                        temp,
                        data.Stride,
                        bitmapProperties);

                    bitmap.UnlockBits(data);

                    _bmp = bmp;
                }
            }
        }

        public Bitmap(D2D1.Bitmap bitmap)
        {
            _bmp = bitmap;
        }

        public static implicit operator D2D1.Bitmap(Bitmap bmp) { return bmp._bmp; }

        #region IBitmap Members

        public override void Dispose()
        {
            _bmp.Dispose();

            base.Dispose();
        }

        public override void Optimize() { throw new NotImplementedException(); }

        public float Dpi => _bmp.DotsPerInch.Width;

        public float Height => _bmp.Size.Height;

        public int PixelHeight => _bmp.PixelSize.Height;

        public int PixelWidth => _bmp.PixelSize.Width;

        public float Width => _bmp.Size.Width;
        public T Unwrap<T>() where T : class { return _bmp as T; }

        #endregion
    }
}