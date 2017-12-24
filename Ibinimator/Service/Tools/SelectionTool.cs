using System;
using System.Collections.Generic;

using Ibinimator.Core.Utility;

using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class SelectionTool : SelectionToolBase<ILayer>
    {
        private readonly Dictionary<string, string> _statuses = new Dictionary<string, string>
        {
            {
                "default",
                "<b>Alt Click</b> to select-behind. " +
                "<b>Shift Click</b> to multi-select."
            },
            {
                "selection",
                "<b>{0}</b> layer(s) selected " +
                "[{1}]"
            },
            {
                "transform",
                "<b>Shift-Drag</b> for constrained movement. " +
                "<b>Alt-Shift-Drag</b> for locally constrained movement."
            }
        };

        private float   _deltaRotation;
        private Vector2 _deltaTranslation;

        private SelectionHandle _handle = 0;

        private (Vector2 position, bool down, long time) _mouse = (Vector2.Zero, false, 0);

        public SelectionTool(IToolManager toolManager)
            : base(toolManager)
        {
            Type = ToolType.Select;

            toolManager.RaiseStatus(new Status(Status.StatusType.Info, _statuses["default"]));
        }

        public override string Cursor { get; protected set; }

        public override float CursorRotate
        {
            get => SelectionManager.SelectionTransform.GetRotation();
            protected set { }
        }

        public GuideManager GuideManager { get; set; } = new GuideManager();

        /// <inheritdoc />
        public override void Attach(IArtContext context)
        {
            context.SelectionManager.SelectionUpdated += OnSelectionUpdated;
            base.Attach(context);
        }

        /// <inheritdoc />
        public override void Detach(IArtContext context)
        {
            context.SelectionManager.SelectionUpdated -= OnSelectionUpdated;
            base.Detach(context);
        }

        public override void KeyDown(IArtContext context, KeyboardEvent evt)
        {
            var key = (Key) evt.KeyCode;

            if (key == Key.Delete)
            {
                var delete = Selection.ToArray();
                SelectionManager.ClearSelection();

                foreach (var layer in delete)
                    Context.HistoryManager.Do(
                        new RemoveLayerCommand(
                            Context.HistoryManager.Position + 1,
                            layer.Parent,
                            layer));

                return;
            }

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt)
                GuideManager.ClearVirtualGuides();

            base.KeyDown(context, evt);
        }

        public override void KeyUp(IArtContext context, KeyboardEvent evt)
        {
            var key = (Key) evt.KeyCode;

            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt)
                GuideManager.ClearVirtualGuides();

            base.KeyUp(context, evt);
        }

        public override void MouseDown(IArtContext context, ClickEvent evt)
        {
            var pos = context.ViewManager.ToArtSpace(evt.Position);

            _deltaTranslation = Vector2.Zero;
            _mouse = (pos, true, Time.Now);

            foreach (var handle in GetHandles(Context.ViewManager.Zoom))
                if (Vector2.Distance(handle.position, pos) < 7.5f)
                {
                    _handle = handle.handle;

                    return;
                }

            base.MouseDown(context, evt);

            if (Context.SelectionManager.Selection.Any())
                _handle = SelectionHandle.Translation;

            UpdateStatus();
        }

        public override void MouseMove(IArtContext context, PointerEvent evt)
        {
            Context.InvalidateRender();

            var pos = context.ViewManager.ToArtSpace(evt.Position);
            var localPos = SelectionManager.ToSelectionSpace(pos);
            var bounds = SelectionManager.SelectionBounds;

            #region cursor

            if (!_mouse.down)
            {
                Cursor = null;

                foreach (var handle in GetHandles(Context.ViewManager.Zoom))
                {
                    if (Vector2.Distance(handle.position, pos) > 7.5f) continue;

                    switch (handle.handle)
                    {
                        case SelectionHandle.Rotation:
                            Cursor = "cursor-rotate";

                            break;
                        case SelectionHandle.Top:
                        case SelectionHandle.Bottom:
                            Cursor = "cursor-resize-ns";

                            break;
                        case SelectionHandle.Left:
                        case SelectionHandle.Right:
                            Cursor = "cursor-resize-ew";

                            break;
                        case SelectionHandle.BottomLeft:
                        case SelectionHandle.TopRight:
                            Cursor = "cursor-resize-nesw";

                            break;
                        case SelectionHandle.TopLeft:
                        case SelectionHandle.BottomRight:
                            Cursor = "cursor-resize-nwse";

                            break;
                        default:

                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            }

            #endregion

            #region transformation

            if (!_mouse.down || !Selection.Any()) return;

            var relativeOrigin = new Vector2(0.5f);

            var scale = new Vector2(1);
            var translate = new Vector2(0);
            var rotate = 0f;
            var shear = 0f;

            #region rotation

            if (_handle == SelectionHandle.Rotation)
            {
                var origin = SelectionManager.FromSelectionSpace(
                    bounds.TopLeft +
                    bounds.Size * relativeOrigin);

                rotate = MathUtils.Angle(pos - origin, false) -
                         MathUtils.Angle(_mouse.position - origin, false);

                #region segmented rotation

                if (evt.ModifierState.Shift)
                {
                    GuideManager.SetGuide(
                        new Guide(
                            0,
                            true,
                            origin,
                            SelectionManager.SelectionTransform.GetRotation(),
                            GuideType.Rotation));

                    _deltaRotation += rotate;
                    rotate = 0;

                    // setting rotate to 0 means that the transformation matrix is
                    // identity, which will cause rendering to stop so we invalidate
                    // the matrix
                    Context.InvalidateRender();

                    if (Math.Abs(_deltaRotation) > MathUtils.PiOverFour / 2)
                    {
                        var delta = Math.Sign(_deltaRotation) *
                                    MathUtils.PiOverFour;

                        rotate = delta;
                        _deltaRotation -= delta;
                    }
                }

                #endregion
            }

            #endregion

            #region translation

            if (_handle == SelectionHandle.Translation)
            {
                translate = pos - _mouse.position;

                #region snapped translation

                if (evt.ModifierState.Shift)
                {
                    var localCenter = bounds.TopLeft +
                                      relativeOrigin * bounds.Size;

                    var center = SelectionManager.FromSelectionSpace(localCenter);
                    var origin = center - _deltaTranslation;

                    if (!GuideManager.VirtualGuidesActive)
                    {
                        var localXaxis = localCenter + Vector2.UnitX;
                        var localYaxis = localCenter + Vector2.UnitY;
                        var xaxis = SelectionManager.FromSelectionSpace(localXaxis);
                        var yaxis = SelectionManager.FromSelectionSpace(localYaxis);

                        Vector2 axisX, axisY;

                        if (evt.ModifierState.Alt) // local axes
                        {
                            axisX = xaxis - center;
                            axisY = yaxis - center;
                        }
                        else
                        {
                            (axisX, axisY) = (Vector2.UnitX, Vector2.UnitY);
                        }

                        GuideManager.SetGuide(
                            new Guide(
                                0,
                                true,
                                origin,
                                MathUtils.Angle(axisX, true),
                                GuideType.Position));

                        GuideManager.SetGuide(
                            new Guide(
                                1,
                                true,
                                origin,
                                MathUtils.Angle(axisY, true),
                                GuideType.Position));
                    }

                    var dest = GuideManager.LinearSnap(pos, origin, GuideType.Position)
                                           .Point;

                    translate = dest - origin - _deltaTranslation;
                }

                #endregion
            }

            #endregion

            #region scaling

            if (_handle.HasFlag(SelectionHandle.Top))
            {
                relativeOrigin.Y = 1.0f;
                scale.Y = (bounds.Bottom - localPos.Y) /
                          bounds.Height;
            }

            if (_handle.HasFlag(SelectionHandle.Left))
            {
                relativeOrigin.X = 1.0f;
                scale.X = (bounds.Right - localPos.X) /
                          bounds.Width;
            }

            if (_handle.HasFlag(SelectionHandle.Right))
            {
                relativeOrigin.X = 0.0f;
                scale.X = (localPos.X - bounds.Left) /
                          bounds.Width;
            }

            if (_handle.HasFlag(SelectionHandle.Bottom))
            {
                relativeOrigin.Y = 0.0f;
                scale.Y = (localPos.Y - bounds.Top) /
                          bounds.Height;
            }

            #region proportional scaling

            if (evt.ModifierState.Shift &&
                (_handle == SelectionHandle.BottomLeft ||
                 _handle == SelectionHandle.BottomRight ||
                 _handle == SelectionHandle.TopRight ||
                 _handle == SelectionHandle.TopLeft))
            {
                var localOrigin = bounds.TopLeft +
                                  relativeOrigin * bounds.Size;
                var localTarget = bounds.TopLeft +
                                  (Vector2.One - relativeOrigin) * bounds.Size;
                var localDest = MathUtils.Scale(localTarget, localOrigin, scale);


                var origin = SelectionManager.FromSelectionSpace(localOrigin);
                var target = SelectionManager.FromSelectionSpace(localTarget);
                var dest = SelectionManager.FromSelectionSpace(localDest);

                var axis = origin - target;

                GuideManager.SetGuide(
                    new Guide(
                        0,
                        true,
                        origin,
                        MathUtils.Angle(axis, true),
                        GuideType.Proportion));

                var snap = GuideManager.LinearSnap(dest, origin, GuideType.Proportion);

                localDest = SelectionManager.ToSelectionSpace(snap.Point);

                scale = (localDest - localOrigin) / (localTarget - localOrigin);
            }

            #endregion

            #endregion

            var size = (bounds.Size + Vector2.One) *
                       SelectionManager.SelectionTransform.GetScale();
            var min = Vector2.One / size;

            // filter out problematic scaling values
            if (float.IsNaN(scale.X) || float.IsInfinity(scale.X)) scale.X = 1;
            if (Math.Abs(scale.X) < Math.Abs(min.X))
                scale.X = min.X * MathUtils.NonZeroSign(scale.X);

            if (float.IsNaN(scale.Y) || float.IsInfinity(scale.Y)) scale.Y = 1;
            if (Math.Abs(scale.Y) < Math.Abs(min.Y))
                scale.Y = min.Y * MathUtils.NonZeroSign(scale.Y);

            SelectionManager.TransformSelection(scale, translate, rotate, shear, relativeOrigin);

            #endregion

            _mouse.position = pos;
            _deltaTranslation += translate;
            _deltaRotation += rotate;
        }

        public override void MouseUp(IArtContext context, ClickEvent evt)
        {
            _mouse.down = false;
            _mouse.position = evt.Position;
            GuideManager.ClearVirtualGuides();

            base.MouseUp(context, evt);
        }

        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            var rect = SelectionManager.SelectionBounds;

            if (rect.IsEmpty) return;

            RenderBoundingBox(target, cache, view);
            RenderPathOutlines(target, cache, view);

            // transform guides
            GuideManager.Render(target, cache, view);

            // handles
            var handles = GetHandles(view.Zoom).ToDictionary();

            using (var pen = target.CreatePen(2, cache.GetBrush(nameof(EditorColors.SelectionHandleOutline))))
            {
                target.DrawLine(handles[SelectionHandle.Top], handles[SelectionHandle.Rotation], pen);

                foreach (var v in handles.Values)
                {
                    target.FillEllipse(v, 5f / view.Zoom, 5f / view.Zoom,
                                       cache.GetBrush(nameof(EditorColors.SelectionHandle)));
                    target.DrawEllipse(v, 5f / view.Zoom, 5f / view.Zoom, pen);
                }
            }
        }

        protected override void OnSelectionUpdated(object sender, EventArgs args)
        {
            base.OnSelectionUpdated(sender, args);
            UpdateStatus();
        }

        private IEnumerable<(SelectionHandle handle, Vector2 position)> GetHandles(float zoom)
        {
            var rect = SelectionManager.SelectionBounds;

            if (rect.IsEmpty) yield break;

            float x1 = rect.Left,
                  y1 = rect.Top,
                  x2 = rect.Right,
                  y2 = rect.Bottom;

            Vector2 Transform(float x, float y)
            {
                return SelectionManager.FromSelectionSpace(new Vector2(x, y));
            }

            yield return (SelectionHandle.TopLeft, Transform(x1, y1));
            yield return (SelectionHandle.TopRight, Transform(x2, y1));
            yield return (SelectionHandle.BottomRight, Transform(x2, y2));
            yield return (SelectionHandle.BottomLeft, Transform(x1, y2));

            var top = Transform((x1 + x2) / 2, y1);

            yield return (SelectionHandle.Top, top);

            yield return (SelectionHandle.Left, Transform(x1, (y1 + y2) / 2));
            yield return (SelectionHandle.Right, Transform(x2, (y1 + y2) / 2));

            var bottom = Transform((x1 + x2) / 2, y2);

            yield return (SelectionHandle.Bottom, bottom);

            var rotate = top - Vector2.Normalize(bottom - top) * 15 / zoom;

            yield return (SelectionHandle.Rotation, rotate);
        }

        private void UpdateStatus()
        {
            if (Selection.Any())
            {
                if (!_mouse.down)
                {
                    var names = Selection.Select(l => l.Name ?? l.DefaultName)
                                         .ToArray();

                    var msg = string.Format(
                        _statuses["selection"],
                        names.Length,
                        string.Join(", ", names));

                    Manager.RaiseStatus(new Status(Status.StatusType.Info, msg));
                }
                else
                {
                    Manager.RaiseStatus(new Status(Status.StatusType.Info, _statuses["transform"]));
                }

                return;
            }

            Manager.RaiseStatus(new Status(Status.StatusType.Info, _statuses["default"]));
        }
    }
}