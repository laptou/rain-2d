using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public class BezierNode : PathNode
    {
        #region Properties

        public float X1 { get => Get<float>(); set => Set(value); }
        public float X2 { get => Get<float>(); set => Set(value); }
        public float Y1 { get => Get<float>(); set => Set(value); }
        public float Y2 { get => Get<float>(); set => Set(value); }

        #endregion Properties
    }

    public class Ellipse : ShapeLayer
    {
        #region Properties

        public float CenterX { get => X + RadiusX; }
        public float CenterY { get => Y + RadiusY; }
        public float RadiusX { get => Get<float>(); set => Set(value); }
        public float RadiusY { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public override Rectangle GetBounds()
        {
            return new Rectangle()
            {
                X1 = CenterX - RadiusX,
                Y1 = CenterY - RadiusY,
                X2 = CenterX + RadiusX,
                Y2 = CenterY + RadiusY
            };
        }

        #endregion Methods
    }

    public class Layer : Model
    {
        #region Properties

        public Layer Mask { get => Get<Layer>(); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); }

        public float Opacity { get => Get<float>(); set => Set(value); }

        public float Rotation { get => Get<float>(); set => Set(value); }
        public ObservableCollection<Layer> SubLayers { get; set; } = new ObservableCollection<Layer>();
        public float X { get => Get<float>(); set => Set(value); }
        public float Y { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public virtual Rectangle GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            Parallel.ForEach(SubLayers, layer =>
            {
                var bounds = layer.GetBounds();

                if (bounds.X1 < x1) x1 = bounds.X1;
                if (bounds.Y1 < y1) y1 = bounds.Y1;
                if (bounds.X2 > x2) x2 = bounds.X2;
                if (bounds.Y2 > y2) y2 = bounds.Y2;
            });

            return new Rectangle() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
        }

        #endregion Methods
    }

    public class Path : ShapeLayer
    {
        #region Properties

        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        #endregion Properties

        #region Methods

        public override Rectangle GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            Parallel.ForEach(Nodes, node =>
            {
                if (node.X < x1) x1 = node.X;
                if (node.Y < y1) y1 = node.Y;
                if (node.X > x2) x2 = node.X;
                if (node.Y > y2) y2 = node.Y;
            });

            return new Rectangle() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
        }

        #endregion Methods
    }

    public class PathNode : Model
    {
        #region Properties

        public float X { get => Get<float>(); set => Set(value); }
        public float Y { get => Get<float>(); set => Set(value); }

        #endregion Properties
    }

    public class Rectangle : ShapeLayer
    {
        #region Properties

        public float X1 { get => Get<float>(); set => Set(value); }
        public float X2 { get => Get<float>(); set => Set(value); }
        public float Y1 { get => Get<float>(); set => Set(value); }
        public float Y2 { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public override Rectangle GetBounds()
        {
            return this;
        }

        #endregion Methods
    }

    public abstract class ShapeLayer : Layer
    {
        #region Properties

        public object StrokeBrush
        {
            get => Get<object>();
            set
            {
                if (value is BrushProperties ||
                    value is BitmapBrushProperties ||
                    value is ImageBrushProperties ||
                    value is LinearGradientBrushProperties ||
                    value is RadialGradientBrushProperties)
                    Set(value);
                else
                    throw new ArgumentException(nameof(value));
            }
        }

        public StrokeStyleProperties StrokeStyle { get => Get<StrokeStyleProperties>(); set => Set(value); }

        public float StrokeWidth { get => Get<float>(); set => Set(value); }

        #endregion Properties
    }
}