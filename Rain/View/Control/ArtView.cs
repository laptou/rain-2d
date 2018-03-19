using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Rain.Core.Utility;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Rain.Core;
using Rain.Core.Input;
using Rain.Core.Model.DocumentGraph;
using Rain.Native;

using Color = Rain.Core.Model.Color;

namespace Rain.View.Control
{
    public class ArtView : D2DImage
    {
        private readonly Dictionary<string, IntPtr> _cursors = new Dictionary<string, IntPtr>();
        private          bool                       _middle  = false;

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

        public ArtContext ArtContext { get; }

        protected override void OnInput(IInputEvent evt)
        {
            var ac = ArtContext;

            switch (evt)
            {
                case ClickEvent clickEvent:
                    if (clickEvent.State)
                        ac.RaiseMouseDown(clickEvent);
                    else
                        ac.RaiseMouseUp(clickEvent);

                    break;
                case PointerEvent pointerEvent:

                    if (pointerEvent.ModifierState.MiddleMouse)
                    {
                        ac.ViewManager.Pan -= pointerEvent.Delta;
                        ac.Invalidate();

                        return;
                    }

                    ac.RaiseMouseMove(pointerEvent);

                    break;
                case TextEvent textEvent:
                    ac.RaiseText(textEvent);

                    break;
                case KeyboardEvent keyEvent:
                    if (keyEvent.State)
                        ac.RaiseKeyDown(keyEvent);
                    else
                        ac.RaiseKeyUp(keyEvent);

                    break;
                case FocusEvent focusEvent:
                    if (focusEvent.State)
                        ac.RaiseGainedFocus(focusEvent);
                    else
                        ac.RaiseLostFocus(focusEvent);

                    break;
                case ScrollEvent scrollEvent:

                    if (scrollEvent.ModifierState.Control)
                    {
                        var position = scrollEvent.Position;

                        var factor = scrollEvent.Delta / 100;
                        if (factor < 0)
                            factor = 2 - Math.Abs(factor);

                        var transform = ac.ViewManager.Transform *
                                        Matrix3x2.CreateScale(factor, position);

                        ac.ViewManager.Pan = transform.Translation;
                        ac.ViewManager.Zoom = transform.GetScale().X;
                    }
                    else
                    {
                        if (scrollEvent.ModifierState.Shift ||
                            scrollEvent.Direction == ScrollDirection.Horizontal)
                            ac.ViewManager.Pan +=
                                new Vector2(scrollEvent.Delta * ac.ViewManager.Zoom / 6, 0);
                        else
                            ac.ViewManager.Pan +=
                                new Vector2(0, scrollEvent.Delta * ac.ViewManager.Zoom / 6);
                    }

                    ac.Invalidate();

                    break;
                case DropEvent dropEvent:
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    OnDropEvent(dropEvent);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private async Task OnDropEvent(DropEvent dropEvent)
        {
            await Task.Run(() =>
                           {
                               var ctx = ArtContext;
                               ctx.SelectionManager.ClearSelection();

                               var root = ctx.ViewManager.Root;
                               var failCount = 0;

                               foreach (var fn in dropEvent.FileNames)
                               {
                                   try
                                   {
                                       var image = ctx.ResourceContext.LoadImageFromFilename(fn);
                                       var pic = new Picture {Image = image};
                                       var center =
                                           new Vector2(pic.Image.Frames[0].Width,
                                                       pic.Image.Frames[0].Height) / 2;
                                       pic.ApplyTransform(
                                           Matrix3x2.CreateTranslation(-center + dropEvent.Position));
                                       root.Add(pic, 0);
                                       pic.Selected = true;
                                   }
                                   catch
                                   {
                                       failCount++;
                                   }
                               }

                               switch (failCount)
                               {
                                   case 0:
                                       ArtContext.Status =
                                           new Status(Status.StatusType.Success, "Images loaded.");

                                       break;
                                   case 1:
                                       ArtContext.Status = new Status(
                                           Status.StatusType.Error,
                                           "1 file failed to load.");

                                       break;
                                   default:
                                       ArtContext.Status = new Status(
                                           Status.StatusType.Error,
                                           $"{failCount} file failed to load.");

                                       break;
                               }
                           });
        }

        protected override IntPtr OnMessage(
            IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
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

        protected override void OnRender(RenderContext target)
        {
            target.Clear(new Color(0.5f));

            var ac = ArtContext;

            if (ac?.ViewManager == null) return;

            target.Transform(ac.ViewManager.Transform, true);

            ac.ViewManager.Render(target, ac.CacheManager);

            ac.ViewManager.Root?.Render(target, ac.CacheManager, ac.ViewManager);

            ac.ToolManager?.Tool?.Render(target, ac.CacheManager, ac.ViewManager);
        }

        private IntPtr LoadCursor(string name)
        {
            var uri = new Uri($"./Resources/Icon/{name}.png", UriKind.Relative);


            if (WindowHelper.GetDpiForWindow(Handle) > 96)
                uri = new Uri($"./Resources/Icon/{name}@2x.png", UriKind.Relative);

            return CursorHelper.CreateCursor(uri, MathUtils.PiOverFour);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _cursors["cursor-resize-ns"] = LoadCursor("cursor-resize-ns");
            _cursors["cursor-resize-ew"] = LoadCursor("cursor-resize-ew");
            _cursors["cursor-resize-nwse"] = LoadCursor("cursor-resize-nwse");
            _cursors["cursor-resize-nesw"] = LoadCursor("cursor-resize-nesw");
            _cursors["cursor-rotate"] = LoadCursor("cursor-rotate");
            _cursors["default"] = CursorHelper.LoadCursor(lpCursorName: (IntPtr) 32512);
        }

        private void OnRenderTargetCreated(object sender, EventArgs eventArgs)
        {
            ArtContext.CacheManager?.ReleaseResources();
            ArtContext.CacheManager?.LoadApplicationResources(RenderContext);

            if (ArtContext.ViewManager?.Root != null)
                ArtContext.CacheManager?.BindLayer(ArtContext.ViewManager.Document.Root);

            InvalidateVisual();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var (_, cursor) in _cursors.AsTuples())
                CursorHelper.DestroyCursor(cursor);
        }
    }
}