﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using Ellipse = SharpDX.Direct2D1.Ellipse;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public sealed class SelectTool : Model.Model, ITool
    {
        public SelectTool(IToolManager toolManager)
        {
            Manager = toolManager;
        }

        private IEnumerable<Layer> Selection => Manager.ArtView.SelectionManager.Selection;

        #region ITool Members

        public Bitmap Cursor => Manager.ArtView.SelectionManager.Cursor;

        public float CursorRotate => Manager.ArtView.SelectionManager.SelectionRotation -
                                     Manager.ArtView.SelectionManager.SelectionShear;

        public bool KeyDown(Key key)
        {
            if (key == Key.Delete)
            {
                var delete = Selection.ToArray();
                Manager.ArtView.SelectionManager.ClearSelection();

                Manager.ArtView.Dispatcher.Invoke(() =>
                {
                    foreach (var layer in delete)
                        layer.Parent.Remove(layer);
                });

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key)
        {
            return false;
        }

        public IToolManager Manager { get; }

        public bool MouseDown(Vector2 pos)
        {
            return false;
        }

        public bool MouseMove(Vector2 pos)
        {
            return false;
        }

        public bool MouseUp(Vector2 pos)
        {
            return false;
        }

        public void ApplyFill(BrushInfo brush)
        {
            foreach (var layer in Selection.SelectMany(l => l.Flatten()))
                if (layer is IFilledLayer filled)
                    filled.FillBrush = brush;
        }

        public void ApplyStroke(BrushInfo brush, StrokeInfo stroke)
        {
            foreach (var layer in Selection.SelectMany(l => l.Flatten()))
            {
                if (layer is IStrokedLayer stroked)
                {
                    stroked.StrokeBrush = brush;
                    stroked.StrokeInfo = stroke;
                }
            }
        }

        public ToolOption[] Options => new ToolOption[0];

        public void Render(RenderTarget target, ICacheManager cache)
        {
            var rect = Manager.ArtView.SelectionManager.SelectionBounds;

            if (rect.IsEmpty) return;

            // draw handles
            var handles = new List<Vector2>();

            float x1 = rect.Left,
                y1 = rect.Top,
                x2 = rect.Right,
                y2 = rect.Bottom;

            handles.Add(new Vector2(x1, y1));
            handles.Add(new Vector2(x2, y1));
            handles.Add(new Vector2(x2, y2));
            handles.Add(new Vector2(x1, y2));
            handles.Add(new Vector2((x1 + x2) / 2, y1));
            handles.Add(new Vector2(x1, (y1 + y2) / 2));
            handles.Add(new Vector2(x2, (y1 + y2) / 2));
            handles.Add(new Vector2((x1 + x2) / 2, y2));
            handles.Add(new Vector2((x1 + x2) / 2, y1 - 10));

            var zoom = MathUtils.GetScale(target.Transform);

            using (var stroke =
                new StrokeStyle1(
                    target.Factory.QueryInterface<Factory1>(),
                    new StrokeStyleProperties1
                    {
                        TransformType = StrokeTransformType.Fixed
                    }))
            {
                foreach (var v in handles.Select(Manager.ArtView.SelectionManager.ToSelectionSpace))
                {
                    var e = new Ellipse(v, 5f / zoom.Y, 5f / zoom.X);
                    target.FillEllipse(e, cache.GetBrush("A1"));
                    target.DrawEllipse(e, cache.GetBrush("L1"), 2, stroke);
                }
            }
        }

        public string Status => "";

        public ToolType Type => ToolType.Select;

        #endregion

        public void Dispose()
        {
        }
    }
}