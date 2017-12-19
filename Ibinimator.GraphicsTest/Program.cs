using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ibinimator.Core;
using Ibinimator.Native;
using Ibinimator.Renderer.Direct2D;

using Color = Ibinimator.Core.Model.Color;
using D2D = SharpDX.Direct2D1;
using DX = SharpDX;
using Format = SharpDX.DXGI.Format;

namespace Ibinimator.GraphicsTest
{
    internal class Program
    {
        private Form                   _form;
        private WndProc                _proc;
        private D2D.WindowRenderTarget _renderTarget;
        public  Direct2DRenderContext  RenderContext { get; set; }

        private void FormOnPaint(object sender, PaintEventArgs e)
        {
            Paint();
            NativeHelper.InvalidateRect(_form.Handle, IntPtr.Zero, false);
        }

        private static void Main() { new Program().Run(); }

        private void Paint()
        {
            _renderTarget.BeginDraw();
            _renderTarget.Clear(new DX.Color());

            var time = DateTime.Now - DateTime.MinValue;

            /*var stops1 = new D2D.GradientStopCollection(
                _renderTarget,
                new[]
                {
                    new D2D.GradientStop {Color = new DX.Color(1f, 0, 0), Position = 0},
                    new D2D.GradientStop
                    {
                        Color = new DX.Color(0, 1f, 0),
                        Position = 0.5f + 0.4f * (float) Math.Sin(time.TotalSeconds)
                    },
                    new D2D.GradientStop {Color = new DX.Color(0, 0, 1f), Position = 1}
                });

            var native = new D2D.LinearGradientBrush(
                _renderTarget,
                new D2D.LinearGradientBrushProperties
                {
                    StartPoint = new DX.Vector2(0, 256),
                    EndPoint = new DX.Vector2(512, 256)
                },
                stops1);*/

            var brush = RenderContext.CreateBrush(
                new[]
                {
                    new GradientStop {Color = new Color(1f, 0, 0), Offset = 0},
                    new GradientStop
                    {
                        Color = new Color(0, 1f, 0),
                        Offset = 0.5f + 0.4f * (float) Math.Sin(time.TotalSeconds)
                    },
                    new GradientStop {Color = new Color(0, 0, 1f), Offset = 1}
                }, 0, 256, 512, 256);

            var native = brush.Unwrap<D2D.LinearGradientBrush>();

            _renderTarget.FillRectangle(
                new DX.RectangleF(0, 0, RenderContext.Width, RenderContext.Height),
                native);

            _renderTarget.EndDraw();

            brush.Dispose();
        }

        private void Run()
        {
            _form = new Form();
            var hWnd = _form.Handle;
            var _d2dFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded);

            var width = (int) (512 * _d2dFactory.DesktopDpi.Width / 96.0);
            var height = (int) (512 * _d2dFactory.DesktopDpi.Height / 96.0);

            var rtp = new D2D.RenderTargetProperties(
                    new D2D.PixelFormat(Format.Unknown,
                                        D2D.AlphaMode.Premultiplied))
                {
                    DpiX = _d2dFactory.DesktopDpi.Width,
                    DpiY = _d2dFactory.DesktopDpi.Height,
                    Type = D2D.RenderTargetType.Hardware
                };

            var hrtp = new D2D.HwndRenderTargetProperties
            {
                Hwnd = hWnd,
                PixelSize = new DX.Size2(width, height),
                PresentOptions = D2D.PresentOptions.None
            };

            _renderTarget = new D2D.WindowRenderTarget(_d2dFactory, rtp, hrtp);

            RenderContext = new Direct2DRenderContext(_renderTarget);

            _form.Size = new Size(512, 512);
            _form.SizeGripStyle = SizeGripStyle.Hide;
            _form.Paint += FormOnPaint;
            _form.ShowDialog();
        }
    }
}