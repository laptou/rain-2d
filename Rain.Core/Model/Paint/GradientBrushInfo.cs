using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

namespace Rain.Core.Model.Paint
{
    public class GradientBrushInfo : BrushInfo
    {
        public GradientBrushInfo() { Stops = new ObservableList<GradientStop>(); }

        public Vector2 EndPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public Vector2 FocusOffset
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public GradientSpace Space
        {
            get => Get<GradientSpace>();
            set => Set(value);
        }

        public SpreadMethod SpreadMethod
        {
            get => Get<SpreadMethod>();
            set => Set(value);
        }

        public Vector2 StartPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public ObservableList<GradientStop> Stops
        {
            get => Get<ObservableList<GradientStop>>();
            set => Set(value);
        }

        public GradientBrushType Type
        {
            get => Get<GradientBrushType>();
            set => Set(value);
        }

        public override IBrush CreateBrush(RenderContext ctx)
        {
            if (Stops.Count == 0) return null;

            IGradientBrush brush;

            switch (Type)
            {
                case GradientBrushType.Linear:
                    brush = ctx.CreateBrush(Stops,
                                            StartPoint.X,
                                            StartPoint.Y,
                                            EndPoint.X,
                                            EndPoint.Y);

                    break;
                case GradientBrushType.Radial:
                    brush = ctx.CreateBrush(Stops,
                                            StartPoint.X,
                                            StartPoint.Y,
                                            EndPoint.X - StartPoint.X,
                                            EndPoint.Y - StartPoint.Y,
                                            FocusOffset.X,
                                            FocusOffset.Y);

                    break;
                default:

                    throw new ArgumentOutOfRangeException();
            }

            brush.Opacity = Opacity;
            brush.Transform = Transform;

            return brush;
        }
    }
}