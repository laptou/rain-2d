using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D1 = SharpDX.Direct2D1;
using DX = SharpDX;
using System.Drawing;

namespace Ibinimator.Renderer.Direct2D
{
    internal class Bitmap : ResourceBase, IBitmap
    {
        private readonly D2D1.Bitmap _bmp;

        public static implicit operator D2D1.Bitmap(Bitmap bmp)
        {
            return bmp._bmp;
        }

        public Bitmap(Direct2DRenderContext ctx, Stream stream)
        {
            using (var bitmap = (System.Drawing.Bitmap)Image.FromStream(stream))
            {
                var sourceArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);

                var bitmapProperties = new D2D1.BitmapProperties(
                    new D2D1.PixelFormat(
                        DX.DXGI.Format.B8G8R8A8_UNorm, 
                        D2D1.AlphaMode.Premultiplied),
                    ctx.Target.DotsPerInch.Width, 
                    ctx.Target.DotsPerInch.Height);

                var data = bitmap.LockBits(sourceArea,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                using (var temp = 
                    new DX.DataStream(
                        data.Scan0, 
                        bitmap.Width * sizeof(int), 
                        true, true))
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

        public override void Dispose()
        {
            _bmp.Dispose();

            base.Dispose();
        }

        public override void Optimize()
        {
            throw new NotImplementedException();
        }

        public float Width => _bmp.Size.Width;

        public float Height => _bmp.Size.Height;

        public int PixelWidth => _bmp.PixelSize.Width;

        public int PixelHeight => _bmp.PixelSize.Height;

        public float Dpi => _bmp.DotsPerInch.Width;
    }
}
