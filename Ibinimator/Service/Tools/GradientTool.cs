using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Tools
{
    public class GradientTool : Core.Model.Model, ITool
    {
        private readonly List<int> _selection = new List<int>();
        public GradientTool(IToolManager manager) { Manager = manager; }

        public Shape CurrentShape =>
            Context.SelectionManager.Selection.LastOrDefault() as Shape;

        private IArtContext Context => Manager.Context;

        private void RenderGradientHandles(
            RenderContext target,
            Matrix3x2 transform)
        {
            if (CurrentShape.Fill is GradientBrushInfo fill)
            {
                using (var pen2 =
                    target.CreatePen(2,
                                     Context.CacheManager.GetBrush("A2")))
                {
                    target.DrawLine(
                        fill.StartPoint,
                        fill.EndPoint,
                        pen2);
                }

                foreach (var stop in fill.Stops)
                {
                    var pos =
                        Vector2.Transform(
                            Vector2.Lerp(
                                fill.StartPoint,
                                fill.EndPoint,
                                stop.Offset),
                            transform);

                    using (var brush = target.CreateBrush(stop.Color))
                    {
                        target.FillEllipse(pos, 4, 4, brush);
                    }

                    using (var pen0 =
                        target.CreatePen(2,
                                         Context.CacheManager.GetBrush(
                                             "L0")))
                    {
                        target.DrawEllipse(pos, 5, 5, pen0);
                    }

                    using (var pen2 =
                        target.CreatePen(1,
                                         Context.CacheManager.GetBrush(
                                             "L2")))
                    {
                        target.DrawEllipse(pos, 5.5f, 5.5f, pen2);
                    }
                }
            }
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            throw new NotImplementedException();
        }

        public void ApplyStroke(PenInfo pen) { throw new NotImplementedException(); }

        public void Dispose() { throw new NotImplementedException(); }

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            throw new NotImplementedException();
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            throw new NotImplementedException();
        }

        public bool MouseDown(Vector2 pos) { throw new NotImplementedException(); }

        public bool MouseMove(Vector2 pos) { throw new NotImplementedException(); }

        public bool MouseUp(Vector2 pos) { throw new NotImplementedException(); }

        public void Render(
            RenderContext target,
            ICacheManager cacheManager)
        {
            if (CurrentShape == null) return;

            RenderGradientHandles(target, CurrentShape.AbsoluteTransform);
        }

        public bool TextInput(string text) { throw new NotImplementedException(); }

        public string Cursor { get; }
        public float CursorRotate { get; }
        public IToolManager Manager { get; }
        public IToolOption[] Options { get; }
        public string Status { get; }
        public ToolType Type { get; }

        #endregion

        #region Nested type: Node

        private class Node
        {
            public Node(
                int index,
                GradientBrushInfo brush,
                ILayer parent)
            {
                Index = index;
                Source = brush;
                Offset = brush.Stops[index].Offset;
                Color = brush.Stops[index].Color;
            }

            public Color Color { get; }
            public int Index { get; }
            public float Offset { get; }

            public GradientBrushInfo Source { get; }
        }

        #endregion
    }
}