using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Input;
using System.Windows.Media;

using Ibinimator.Core;

using Color = Ibinimator.Core.Model.Color;
using D2D = SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public class ArtView : D2DImage2
    {
        private readonly ISet<Key> _keys = new HashSet<Key>();

        private Vector2 _lastPosition;

        public ArtView()
        {
            // RenderMode = RenderMode.Manual;
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Focusable = true;

            RenderTargetCreated += OnRenderTargetCreated;
            ArtContext = new ArtContext(this);
        }

        public ArtContext ArtContext { get; }

        protected override void HandleInput(InputEvent evt)
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

                    ac.ViewManager.Pan += new Vector2(0, evt.ScrollDelta * ac.ViewManager.Zoom);
                    ac.InvalidateSurface();

                    break;
                case InputEventType.ScrollHorizontal:
                    ac.ViewManager.Pan += new Vector2(evt.ScrollDelta * ac.ViewManager.Zoom, 0);
                    ac.InvalidateSurface();

                    break;
                default:

                    throw new ArgumentOutOfRangeException();
            }
        }

        /**
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            // quickfix b/c asychronous events mean we can't use Handled
            // which creates the problem of backspace registering as 
            // text input for some reason
            if (e.Text == "\b" || e.Text == "")
            {
                _eventFlag.Set();
                return;
            }

            lock (_events)
            {
                _events.Enqueue(
                    new InputEvent(
                        InputEventType.TextInput,
                        e.Text));
            }

            _eventFlag.Set();
        }
    */

        protected override void Render(RenderContext target)
        {
            target.Clear(new Color(0.5f));

            if (ArtContext.ViewManager == null) return;

            var ac = ArtContext;

            target.Transform(ac.ViewManager.Transform, true);

            ac.ViewManager.Render(target, ac.CacheManager);

            ac.ViewManager.Root.Render(target, ac.CacheManager, ac.ViewManager);

            if (ac.ToolManager?.Tool == null) return;

            ac.ToolManager.Tool.Render(target, ac.CacheManager, ac.ViewManager);

            if (ac.ToolManager.Tool.Cursor == null)
            {
                Cursor = Cursors.Arrow;

                return;
            }

            Cursor = Cursors.None;

            target.Transform(
                Matrix3x2.CreateRotation(ac.ToolManager.Tool.CursorRotate,
                                         new Vector2(8)) *
                Matrix3x2.CreateTranslation(_lastPosition - new Vector2(8)),
                true);

            target.DrawBitmap(ac.CacheManager.GetBitmap(ac.ToolManager.Tool.Cursor));
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