using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Ibinimator.Core;
using Ibinimator.Core.Utility;
using Ibinimator.Native;

using Color = Ibinimator.Core.Model.Color;
using D2D = SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage2
    {
        private Vector2 _lastPosition;
        private Dictionary<string, IntPtr> _cursors = new Dictionary<string, IntPtr>();

        public ArtView()
        {
            // RenderMode = RenderMode.Manual;
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Focusable = true;

            RenderTargetCreated += OnRenderTargetCreated;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ArtContext = new ArtContext(this);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var (_, cursor) in _cursors.AsTuples())
                CursorHelper.DestroyCursor(cursor);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _cursors["cursor-resize-ns"] = LoadCursor("cursor-resize-ns");
            _cursors["cursor-resize-ew"] = LoadCursor("cursor-resize-ew");
            _cursors["cursor-resize-nwse"] = LoadCursor("cursor-resize-nwse");
            _cursors["cursor-resize-nesw"] = LoadCursor("cursor-resize-nesw");
            _cursors["cursor-rotate"] = LoadCursor("cursor-rotate");
            _cursors["default"] = CursorHelper.LoadCursor(lpCursorName: (IntPtr)32512);
        }

        private IntPtr LoadCursor(string name)
        {
            var uri = new Uri($"./Resources/Icon/{name}.png", UriKind.Relative);


            if (WindowHelper.GetDpiForWindow(Handle) > 96)
                uri = new Uri($"./Resources/Icon/{name}@2x.png", UriKind.Relative);
            
            return CursorHelper.CreateCursor(uri, MathUtils.PiOverFour);
        }

        public ArtContext ArtContext { get; }

        protected override IntPtr OnMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.SetCursor:
                    CursorHelper.SetCursor(ArtContext.ToolManager.Tool.Cursor != null
                                               ? _cursors[ArtContext.ToolManager.Tool.Cursor]
                                               : _cursors["default"]);

                    break;

                default:
                    return base.OnMessage(hWnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }

        protected override void OnInput(InputEvent evt)
        {
            var ac = ArtContext;
            var pos = ac.ViewManager.ToArtSpace(evt.Position);

            switch (evt.Type)
            {
                case InputEventType.MouseDown:
                    _lastPosition = evt.Position;
                    ac.ToolManager.MouseDown(pos, evt.State);

                    break;
                case InputEventType.MouseUp:
                    _lastPosition = evt.Position;
                    ac.ToolManager.MouseUp(pos, evt.State);

                    break;
                case InputEventType.MouseMove:
                    _lastPosition = evt.Position;
                    ac.ToolManager.MouseMove(pos, evt.State);

                    break;
                case InputEventType.TextInput:
                    ac.ToolManager.TextInput(evt.Text);

                    break;
                case InputEventType.KeyUp:
                    ac.ToolManager.KeyUp(evt.Key, evt.State);

                    break;
                case InputEventType.KeyDown:
                    ac.ToolManager.KeyDown(evt.Key, evt.State);

                    break;
                case InputEventType.ScrollVertical:
                    if (evt.State.Shift)
                        goto case InputEventType.ScrollHorizontal;

                    if (evt.State.Control)
                        ac.ViewManager.Zoom *= (float)Math.Pow(10, evt.ScrollDelta / 100) / 10f;

                    ac.ViewManager.Pan += new Vector2(0, evt.ScrollDelta * ac.ViewManager.Zoom / 6);
                    ac.InvalidateSurface();

                    break;
                case InputEventType.ScrollHorizontal:
                    ac.ViewManager.Pan += new Vector2(evt.ScrollDelta * ac.ViewManager.Zoom / 6, 0);
                    ac.InvalidateSurface();

                    break;
                default:

                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnRender(RenderContext target)
        {
            target.Clear(new Color(0.5f));

            if (ArtContext.ViewManager == null) return;

            var ac = ArtContext;

            target.Transform(ac.ViewManager.Transform, true);

            ac.ViewManager.Render(target, ac.CacheManager);

            ac.ViewManager.Root.Render(target, ac.CacheManager, ac.ViewManager);

            if (ac.ToolManager?.Tool == null) return;

            ac.ToolManager.Tool.Render(target, ac.CacheManager, ac.ViewManager);

            //if (ac.ToolManager.Tool.Cursor == null)
            //{
            //    Cursor = Cursors.Arrow;

            //    return;
            //}

            //Cursor = Cursors.None;

            //target.Transform(
            //    Matrix3x2.CreateRotation(ac.ToolManager.Tool.CursorRotate,
            //                             new Vector2(8)) *
            //    Matrix3x2.CreateTranslation(_lastPosition - new Vector2(8)),
            //    true);

            //target.DrawBitmap(ac.CacheManager.GetBitmap(ac.ToolManager.Tool.Cursor));
        }

        private void OnRenderTargetCreated(object sender, EventArgs eventArgs)
        {
            ArtContext.CacheManager?.ResetAll();
            ArtContext.CacheManager?.LoadBrushes(RenderContext);
            ArtContext.CacheManager?.LoadBitmaps(RenderContext);

            if (ArtContext.ViewManager?.Root != null)
                ArtContext.CacheManager?.BindLayer(ArtContext.ViewManager.Document.Root);

            InvalidateVisual();
        }
    }
}