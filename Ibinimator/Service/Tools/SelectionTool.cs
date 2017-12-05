using System;
using System.Collections.Generic;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class SelectionTool : Model, ITool
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

        private float _accumRotation;
        private Vector2 _deltaTranslation;

        private int _depth = 1;
        private SelectionHandle _handle = 0;

        private (bool alt, bool shift, bool ctrl) _modifiers = (false, false, false);
        private (Vector2 position, bool down, long time) _mouse = (Vector2.Zero, false, 0);

        public SelectionTool(IToolManager toolManager, ISelectionManager selectionManager)
        {
            Manager = toolManager;

            toolManager.RaiseStatus(new Status(Status.StatusType.Info, _statuses["default"]));

            selectionManager.Updated += OnSelectionUpdated;
        }

        public GuideManager GuideManager { get; set; } = new GuideManager();


        private IArtContext Context => Manager.Context;

        private IEnumerable<ILayer> Selection => Manager.Context.SelectionManager.Selection;
        private ISelectionManager SelectionManager => Manager.Context.SelectionManager;

        public void SetHandle(SelectionHandle value) { _handle = value; }

        private IEnumerable<(SelectionHandle handle, Vector2 position)> GetHandlePositions(float zoom)
        {
            var rect = Manager.Context.SelectionManager.SelectionBounds;

            float x1 = rect.Left,
                  y1 = rect.Top,
                  x2 = rect.Right,
                  y2 = rect.Bottom;

            Vector2 Transform(float x, float y)
            {
                return Manager.Context.SelectionManager.FromSelectionSpace(new Vector2(x, y));
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

        private void OnSelectionUpdated(object sender, EventArgs args)
        {
            _depth = Selection.Any() ? 1 : Selection.Select(l => l.Depth).Min();
            UpdateStatus();
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

        #region ITool Members

        public void ApplyFill(IBrushInfo brush)
        {
            if (!Selection.Any())
                return;

            var targets =
                Selection.SelectMany(l => l.Flatten())
                         .OfType<IFilledLayer>()
                         .ToArray();

            var command = new ApplyFillCommand(
                Manager.Context.HistoryManager.Position + 1,
                targets,
                brush,
                targets.Select(t => t.Fill).ToArray());

            Manager.Context.HistoryManager.Merge(command, 500);
        }

        public void ApplyStroke(IPenInfo pen)
        {
            if (!Selection.Any())
                return;

            var targets =
                Selection.SelectMany(l => l.Flatten())
                         .OfType<IStrokedLayer>()
                         .ToArray();

            var command = new ApplyStrokeCommand(
                Manager.Context.HistoryManager.Position + 1,
                targets,
                pen,
                targets.Select(t => t.Stroke).ToArray());

            var old = Manager.Context.HistoryManager.Current;

            if (old is ApplyStrokeCommand oldStrokeCommand &&
                command.Time - old.Time <= 500)
            {
                Manager.Context.HistoryManager.Pop();

                command = new ApplyStrokeCommand(
                    command.Id,
                    command.Targets,
                    command.NewStroke,
                    oldStrokeCommand.OldStrokes);
            }

            Manager.Context.HistoryManager.Do(command);
        }

        public void Dispose() { Manager.Context.SelectionManager.Updated -= OnSelectionUpdated; }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            var delete = Selection.ToArray();
            Manager.Context.SelectionManager.ClearSelection();

            foreach (var layer in delete)
                Manager.Context.HistoryManager.Do(
                    new RemoveLayerCommand(
                        Manager.Context.HistoryManager.Position + 1,
                        layer.Parent,
                        layer));

            if (modifiers.HasFlag(ModifierKeys.Alt))
                _modifiers = (true, _modifiers.shift, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Shift))
                _modifiers = (_modifiers.alt, true, _modifiers.ctrl);

            if (modifiers.HasFlag(ModifierKeys.Control))
                _modifiers = (_modifiers.alt, _modifiers.shift, true);

            return true;
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    _modifiers = (false, _modifiers.shift, _modifiers.ctrl);
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    _modifiers = (_modifiers.alt, false, _modifiers.ctrl);
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _modifiers = (_modifiers.alt, _modifiers.shift, false);
                    break;
            }

            return true;
        }


        public bool MouseDown(Vector2 pos)
        {
            UpdateStatus();

            var deltaTime = Time.Now - _mouse.time;
            _deltaTranslation = Vector2.Zero;
            _mouse = (pos, true, Time.Now);

            #region handle testing

            SetHandle(0);

            var localPos = SelectionManager.ToSelectionSpace(pos);
            var localScale = SelectionManager.SelectionTransform.GetScale();
            var radiusx = 7.5f / localScale.X;
            var radiusy = 7.5f / localScale.Y;

            if (Math.Abs(localPos.X - SelectionManager.SelectionBounds.Left) < radiusx)
            {
                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Top) < radiusy)
                    SetHandle(SelectionHandle.TopLeft);

                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Center.Y) < radiusy)
                    SetHandle(SelectionHandle.Left);

                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Bottom) < radiusy)
                    SetHandle(SelectionHandle.BottomLeft);
            }

            if (Math.Abs(localPos.X - SelectionManager.SelectionBounds.Center.X) < radiusx)
            {
                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Top) < radiusy)
                    SetHandle(SelectionHandle.Top);

                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Bottom) < radiusy)
                    SetHandle(SelectionHandle.Bottom);
            }

            if (Math.Abs(localPos.X - SelectionManager.SelectionBounds.Right) < radiusx)
            {
                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Top) < radiusy)
                    SetHandle(SelectionHandle.TopRight);

                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Center.Y) < radiusy)
                    SetHandle(SelectionHandle.Right);

                if (Math.Abs(localPos.Y - SelectionManager.SelectionBounds.Bottom) < radiusy)
                    SetHandle(SelectionHandle.BottomRight);
            }

            var bounds = SelectionManager.SelectionBounds;

            var vertical = SelectionManager.FromSelectionSpace(bounds.BottomCenter) -
                           SelectionManager.FromSelectionSpace(bounds.TopCenter);

            var rotationHandle = SelectionManager.FromSelectionSpace(bounds.TopCenter) -
                                 Vector2.Normalize(vertical) * 15;

            if (Vector2.Distance(rotationHandle, pos) < 7.5)
                SetHandle(SelectionHandle.Rotation);

            if (_handle != 0)
                return true;

            #endregion

            if (deltaTime < 500)
                _depth++;

            #region hit testing

            // for every element in the scene, perform a hit-test
            var root = Context.ViewManager.Root;

            // start by hit-testing in the existing selection, and if we find nothing,
            // then hit-test in the root
            ILayer hit = null;

            foreach (var layer in root.Flatten(_depth).Skip(1))
            {
                var test = layer.HitTest<ILayer>(Context.CacheManager, pos, 0);

                if (test == null) continue;

                hit = test;

                if (hit.Depth < _depth) continue;

                if (_modifiers.alt && hit.Selected) continue;

                break;
            }

            #endregion

            if (deltaTime < 500 && hit == null)
                _depth--;

            if (hit != null && _handle == 0)
                SetHandle(SelectionHandle.Translation);

            if (!_modifiers.shift && hit?.Selected != true)
                SelectionManager.ClearSelection();

            if (hit != null)
                hit.Selected = true;

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            Manager.Context.InvalidateSurface();

            var localPos = SelectionManager.ToSelectionSpace(pos);
            var bounds = SelectionManager.SelectionBounds;

            #region cursor

            if (!_mouse.down)
            {
                Cursor = null;
                
                var handles = GetHandlePositions(Manager.Context.ViewManager.Zoom);

                foreach (var handle in handles)
                {
                    if (Vector2.Distance(handle.position, _mouse.position) > 7.5f) continue;

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

            if (!_mouse.down) return false;
            if (!Selection.Any()) return false;

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

                if (_modifiers.shift)
                {
                    GuideManager.AddGuide(
                        new Guide(
                            0,
                            true,
                            origin,
                            SelectionManager.SelectionTransform.GetRotation(),
                            GuideType.Rotation));

                    _accumRotation += rotate;
                    rotate = 0;

                    // setting rotate to 0 means that the transformation matrix is
                    // identity, which will cause rendering to stop so we invalidate
                    // the matrix
                    Context.InvalidateSurface();

                    if (Math.Abs(_accumRotation) > MathUtils.PiOverFour / 2)
                    {
                        var delta = Math.Sign(_accumRotation) *
                                    MathUtils.PiOverFour;

                        rotate = delta;
                        _accumRotation -= delta;
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

                if (_modifiers.shift)
                {
                    var localCenter = bounds.TopLeft +
                                      relativeOrigin * bounds.Size;

                    var center = SelectionManager.FromSelectionSpace(localCenter);
                    var origin = center - _deltaTranslation;

                    var localXaxis = localCenter + Vector2.UnitX;
                    var localYaxis = localCenter + Vector2.UnitY;
                    var xaxis = SelectionManager.FromSelectionSpace(localXaxis);
                    var yaxis = SelectionManager.FromSelectionSpace(localYaxis);

                    Vector2 axisX, axisY;

                    if (_modifiers.alt) // local axes
                    {
                        axisX = xaxis - center;
                        axisY = yaxis - center;
                    }
                    else
                    {
                        (axisX, axisY) = (Vector2.UnitX, Vector2.UnitY);
                    }

                    GuideManager.AddGuide(
                        new Guide(
                            0,
                            true,
                            origin,
                            MathUtils.Angle(axisX, true),
                            GuideType.Position));

                    GuideManager.AddGuide(
                        new Guide(
                            1,
                            true,
                            origin,
                            MathUtils.Angle(axisY, true),
                            GuideType.Position));

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

            if (_modifiers.shift &&
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

                GuideManager.AddGuide(
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

            SelectionManager.Transform(scale, translate, rotate, shear, relativeOrigin);

            _mouse.position = pos;

            #endregion

            return true;
        }

        public bool MouseUp(Vector2 pos)
        {
            _mouse.position = pos;
            _mouse.down = false;
            Context.InvalidateSurface();
            return true;
        }

        public IBrushInfo ProvideFill() { throw new NotImplementedException(); }
        public IPenInfo ProvideStroke() { throw new NotImplementedException(); }

        public void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            var rect = Manager.Context.SelectionManager.SelectionBounds;

            if (rect.IsEmpty) return;

            // path outlines
            foreach (var shape in Selection.OfType<IGeometricLayer>())
            {
                target.Transform(shape.AbsoluteTransform);

                using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
                {
                    target.DrawGeometry(Context.CacheManager.GetGeometry(shape), pen);
                }

                target.Transform(MathUtils.Invert(shape.AbsoluteTransform));
            }

            // bounding box outlines
            target.Transform(SelectionManager.SelectionTransform);

            using (var pen = target.CreatePen(1, cache.GetBrush(nameof(EditorColors.SelectionOutline))))
            {
                target.DrawRectangle(SelectionManager.SelectionBounds, pen);
            }

            target.Transform(MathUtils.Invert(SelectionManager.SelectionTransform));

            // transform guides
            foreach (var guide in GuideManager.GetGuides(GuideType.All))
            {
                target.PushEffect(target.CreateEffect<IGlowEffect>());

                var brush = cache.GetBrush(nameof(EditorColors.Guide));

                if (guide.Type.HasFlag(GuideType.Linear))
                {
                    var origin = guide.Origin;
                    var slope = Math.Tan(guide.Angle);
                    var diagonal = target.Height / target.Width;
                    Vector2 p1, p2;

                    if (slope > diagonal)
                    {
                        p1 = new Vector2(
                            (float) (origin.X + (origin.Y - target.Height) / slope),
                            target.Height);
                        p2 = new Vector2((float) (origin.X + origin.Y / slope), 0);
                    }
                    else
                    {
                        p1 = new Vector2(
                            target.Width,
                            (float) (origin.Y + (origin.X - target.Width) * slope));
                        p2 = new Vector2(0, (float) (origin.Y + origin.X * slope));
                    }

                    using (var pen = target.CreatePen(2, brush))
                    {
                        target.DrawLine(p1, p2, pen);
                    }
                }

                if (guide.Type.HasFlag(GuideType.Radial))
                {
                    var origin = guide.Origin;
                    var axes = new[]
                    {
                        guide.Angle,
                        guide.Angle + MathUtils.PiOverFour * 1,
                        guide.Angle + MathUtils.PiOverFour * 2,
                        guide.Angle + MathUtils.PiOverFour * 3
                    };

                    using (var pen = target.CreatePen(1, brush))
                    {
                        target.DrawEllipse(origin, 20, 20, pen);

                        foreach (var x in axes)
                            target.DrawLine(origin + MathUtils.Angle(x) * 20,
                                            origin - MathUtils.Angle(x) * 20,
                                            pen);
                    }

                    using (var pen = target.CreatePen(2, brush))
                    {
                        target.DrawLine(origin - MathUtils.Angle(-axes[2]) * 25,
                                        origin,
                                        pen);
                    }
                }

                target.PopEffect();
            }

            GuideManager.ClearVirtualGuides();

            // handles
            var handles = GetHandlePositions(view.Zoom).ToDictionary();

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

        public bool TextInput(string text) { return false; }

        public string Cursor { get; set; }

        public float CursorRotate => SelectionManager.SelectionTransform.GetRotation();

        public IToolManager Manager { get; }

        public ToolOptions Options { get; } = new ToolOptions();

        public ToolType Type => ToolType.Select;

        #endregion
    }
}