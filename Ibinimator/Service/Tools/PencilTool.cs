﻿using System;
using System.Collections.Generic;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service.Tools
{
    public sealed class PencilTool : Core.Model.Model, ITool
    {
        private bool _alt;
        private bool _down;
        private Vector2 _lastPos;
        private bool _moved;
        private bool _shift;

        public PencilTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        public Path CurrentPath => Manager.Context.SelectionManager.Selection.LastOrDefault() as Path;

        public IToolOption[] Options => new IToolOption[0]; // TODO: add actual tool options

        private IArtContext Context => Manager.Context;

        private IContainerLayer Root => Context.ViewManager.Root;

        private Vector2 Constrain(Vector2 pos)
        {
            var lastNode = CurrentPath.Instructions.OfType<CoordinatePathInstruction>().Last();
            var lpos = Vector2.Transform(lastNode.Position, CurrentPath.AbsoluteTransform);

            var delta = pos - lpos;

            if (Math.Abs(delta.Y / delta.X) > MathUtils.Sqrt3)
                delta = new Vector2(0, delta.Y);
            else if (Math.Abs(delta.Y / delta.X) > MathUtils.InverseSqrt3)
                delta = MathUtils.Project(delta, new Vector2(1, Math.Sign(delta.Y / delta.X)));
            else
                delta = new Vector2(delta.X, 0);

            return lpos + delta;
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            throw new NotImplementedException();
        }

        public void ApplyStroke(PenInfo pen)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            _shift = modifiers.HasFlag(ModifierKeys.Shift);
            _alt = modifiers.HasFlag(ModifierKeys.Alt);

            switch (key)
            {
                case Key.Escape:
                    Context.SelectionManager.ClearSelection();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            _shift = modifiers.HasFlag(ModifierKeys.Shift);
            _alt = modifiers.HasFlag(ModifierKeys.Alt);

            return true;
        }

        public bool MouseDown(Vector2 pos)
        {
            if (CurrentPath == null)
            {
                var hit = Root.Hit<Path>(Context.CacheManager, pos, true);

                if (hit != null)
                {
                    hit.Selected = true;
                    return true;
                }

                Context.SelectionManager.ClearSelection();

                var path = new Path
                {
                    Fill = Context.BrushManager.Fill,
                    Stroke = Context.BrushManager.Stroke
                };

                Context.HistoryManager.Do(
                    new AddLayerCommand(Context.HistoryManager.Position + 1,
                        Root,
                        path));

                path.Selected = true;
            }

            _moved = false;
            _down = true;

            Context.SelectionManager.Update(true);

            return true;
        }

        public bool MouseMove(Vector2 pos)
        {
            _lastPos = pos;

            _moved = true;

            Context.InvalidateSurface();

            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            /*
            if (!_moved)
            {
                var tpos =
                    Vector2.Transform(pos, MathUtils.Invert(CurrentPath.AbsoluteTransform));

                var node =
                    CurrentPath
                        .Instructions
                        .OfType<CoordinatePathInstruction>()
                        .FirstOrDefault(n => (n.Position - tpos).Length() < 5);

                if (node != null)
                {
                    if (_alt)
                        Context.HistoryManager.Do(
                            new ModifyPathCommand(
                                Context.HistoryManager.Position + 1,
                                CurrentPath,
                                new[] {CurrentPath.Instructions.IndexOf(node)},
                                ModifyPathCommand.NodeOperation.Remove));
                }
                else
                {
                    CoordinatePathInstruction newNode;

                    if (_shift)
                    {
                        var cpos =
                            Vector2.Transform(
                                Constrain(pos),
                                MathUtils.Invert(CurrentPath.AbsoluteTransform));

                        newNode = new LinePathInstruction(cpos);
                    }
                    else
                    {
                        newNode = new LinePathInstruction(tpos);
                    }

                    if(!CurrentPath.Instructions.Any())
                        newNode = new MovePathInstruction(newNode.Position);

                    Context.HistoryManager.Do(
                        new ModifyPathCommand(
                            Context.HistoryManager.Position + 1,
                            CurrentPath,
                            new[] { newNode },
                            CurrentPath.Instructions.Count
                            ));
                }
            }
            */
            Context.InvalidateSurface();

            _down = false;
            return true;
        }

        public void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            if (CurrentPath == null) return;

            var transform = CurrentPath.AbsoluteTransform;
            target.Transform(transform);

            using (var geom = cache.GetGeometry(CurrentPath))
            using (var pen = target.CreatePen(1, cache.GetBrush("A2")))
            {
                target.DrawGeometry(geom, pen);
            }

            target.Transform(MathUtils.Invert(transform));

            var figures = CurrentPath.Instructions.Split(n => n is ClosePathInstruction);

            foreach (var figure in figures)
            {
                var nodes = figure.Cast<CoordinatePathInstruction>().ToArray();

                for (var i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];

                    var pos = Vector2.Transform(node.Position, transform);
                    var zoom = Context.ViewManager.Zoom;

                    var rect = new RectangleF(pos.X - 4f, pos.Y - 4f, 8 / zoom, 8 / zoom);

                    if (_down)
                        target.FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height,
                            rect.Contains(_lastPos) ? cache.GetBrush("A4") : cache.GetBrush("L1"));
                    else
                        target.FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height,
                            rect.Contains(_lastPos) ? cache.GetBrush("A3") : cache.GetBrush("L1"));

                    using (var pen = target.CreatePen(1,
                        i == 0 ? cache.GetBrush("A4") : cache.GetBrush("A2")))
                    {
                        target.DrawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, pen);
                    }
                }
            }
        }

        public bool TextInput(string text)
        {
            return false;
        }

        public string CursorImage => null;

        public float CursorRotate => 0;

        public IToolManager Manager { get; }

        public string Status => "";

        public ToolType Type => ToolType.Pencil;

        #endregion
    }
}