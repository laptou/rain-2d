using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Utility;

using GradientStop = Ibinimator.Core.Model.Paint.GradientStop;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public class GradientBrushInfo : BrushInfo
    {
        public GradientBrushInfo() { Stops = new ObservableList<GradientStop>(); }

        public Vector2 EndPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public SpreadMethod SpreadMethod
        {
            get => Get<SpreadMethod>();
            set => Set(value);
        }

        public Vector2 Focus
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public GradientBrushType GradientType
        {
            get => Get<GradientBrushType>();
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

        public override IBrush CreateBrush(RenderContext ctx)
        {
            if (Stops.Count == 0) return null;

            IGradientBrush brush;

            switch (GradientType)
            {
                case GradientBrushType.Linear:
                    brush = ctx.CreateBrush(
                        Stops,
                        StartPoint.X,
                        StartPoint.Y,
                        EndPoint.X,
                        EndPoint.Y);

                    break;
                case GradientBrushType.Radial:
                    brush = ctx.CreateBrush(
                        Stops,
                        StartPoint.X,
                        StartPoint.Y,
                        EndPoint.X - StartPoint.X,
                        EndPoint.Y - StartPoint.Y,
                        Focus.X,
                        Focus.Y);

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