using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ibinimator.Renderer.WPF;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ibinimator.Core;
using Ibinimator.Renderer.Direct2D;
using Ibinimator.Service;
using SharpDX.DXGI;
using Color = Ibinimator.Core.Model.Color;
using D2D = SharpDX.Direct2D1;
using Matrix3x2 = System.Numerics.Matrix3x2;
using Vector2 = System.Numerics.Vector2;

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

        private void OnRenderTargetCreated(object sender, EventArgs eventArgs)
        {
            ArtContext.CacheManager?.ResetAll();
            ArtContext.CacheManager?.LoadBrushes(RenderContext);
            ArtContext.CacheManager?.LoadBitmaps(RenderContext);

            if (ArtContext.ViewManager?.Root != null)
                ArtContext.CacheManager?.BindLayer(ArtContext.ViewManager.Document.Root);

            InvalidateVisual();
        }

        public ArtContext ArtContext { get; }
        
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

        protected override void HandleInput(InputEvent evt)
        {
            var ac = ArtContext;
            var pos = ac.ViewManager.ToArtSpace(evt.Position);

            switch (evt.Type)
            {
                case InputEventType.MouseDown:
                    ac.ToolManager.MouseDown(pos);
                    break;
                case InputEventType.MouseUp:
                    ac.ToolManager.MouseUp(pos);
                    break;
                case InputEventType.MouseMove:
                    ac.ToolManager.MouseMove(pos);
                    break;

                case InputEventType.TextInput:
                    ac.ToolManager.TextInput(evt.Text);
                    break;
                case InputEventType.KeyUp:
                    ac.ToolManager.KeyUp(evt.Key, evt.Modifier);
                    break;
                case InputEventType.KeyDown:
                    ac.ToolManager.KeyDown(evt.Key, evt.Modifier);
                    break;
                //case InputEventType.Scroll:
                //    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    
}