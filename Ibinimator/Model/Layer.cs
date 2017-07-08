using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using SharpDX;

namespace Ibinimator.Model
{
    public enum BrushType
    {
        Color, Bitmap, Image, LinearGradient, RadialGradient
    }

    public class BezierNode : PathNode
    {
        #region Properties

        public float X1 { get => Get<float>(); set => Set(value); }
        public float X2 { get => Get<float>(); set => Set(value); }
        public float Y1 { get => Get<float>(); set => Set(value); }
        public float Y2 { get => Get<float>(); set => Set(value); }

        #endregion Properties
    }

    public class BrushInfo : Model
    {
        #region Constructors

        public BrushInfo(BrushType brushType)
        {
            BrushType = brushType;

            Opacity = 1;

            if (IsGradient)
                Stops = new List<GradientStop>();
        }

        #endregion Constructors

        #region Properties

        public RawVector2 StartPoint
        {
            get => Get<RawVector2>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public byte[] Bitmap { get; set; }

        public BrushType BrushType { get => Get<BrushType>(); private set => Set(value); }

        public RawMatrix3x2 Transform { get => Get<RawMatrix3x2>(); set => Set(value); }

        public RawColor4 Color
        {
            get => Get<RawColor4>();
            set { if (!IsGradient && !IsImage) Set(value); else throw new InvalidOperationException("Not a color brush."); }
        }

        public RawVector2 EndPoint
        {
            get => Get<RawVector2>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public float Opacity { get => Get<float>(); set => Set(value); }

        public List<GradientStop> Stops
        {
            get => Get<List<GradientStop>>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        private bool IsGradient => BrushType == BrushType.LinearGradient || BrushType == BrushType.RadialGradient;
        private bool IsImage => BrushType == BrushType.Image || BrushType == BrushType.Bitmap;

        #endregion Properties
    }

    public class Ellipse : Shape
    {
        #region Properties

        public float CenterX { get => X + RadiusX; }
        public float CenterY { get => Y + RadiusY; }
        public float RadiusX { get => Get<float>(); set => Set(value); }
        public float RadiusY { get => Get<float>(); set => Set(value); }
        public override String DefaultName => "Ellipse";

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            return new RectangleF()
            {
                Left = CenterX - RadiusX,
                Top = CenterY - RadiusY,
                Right = CenterX + RadiusX,
                Bottom = CenterY + RadiusY
            };
        }

        #endregion Methods
    }

    public class Group : Layer
    {
        public override String DefaultName => "Group";
    }

    public class Layer : Model
    {
        public Layer()
        {
            Opacity = 1;

            SubLayers.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (Layer layer in e.NewItems)
                layer.PropertyChanged += OnSubLayerChanged;

            if(e.Action == NotifyCollectionChangedAction.Remove)
                foreach (Layer layer in e.OldItems)
                    layer.PropertyChanged -= OnSubLayerChanged;
        }

        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);
        }

        #region Properties

        public Layer Mask { get => Get<Layer>(); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); }

        public virtual String DefaultName => "Layer";

        public float Opacity { get => Get<float>(); set => Set(value); }

        public Matrix3x2 Transform { get => Get<RawMatrix3x2>(); set => Set(value); }

        public ObservableCollection<Layer> SubLayers { get; set; } = new ObservableCollection<Layer>();

        public float X { get => Get<float>(); set => Set(value); }

        public float Y { get => Get<float>(); set => Set(value); }

        public bool Selected { get => Get<bool>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public virtual RectangleF GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            Parallel.ForEach(SubLayers, layer =>
            {
                var bounds = layer.GetBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2, y2);
        }

        #endregion Methods
    }

    public class Path : Shape
    {
        #region Properties

        public override String DefaultName => "Path";
        public ObservableCollection<PathNode> Nodes { get; set; } = new ObservableCollection<PathNode>();

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            Parallel.ForEach(Nodes, node =>
            {
                if (node.X < x1) x1 = node.X;
                if (node.Y < y1) y1 = node.Y;
                if (node.X > x2) x2 = node.X;
                if (node.Y > y2) y2 = node.Y;
            });

            return new RectangleF(x1, y1, x2, y2);
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

    public class Rectangle : Shape
    {
        #region Properties

        public override String DefaultName => "Rectangle";
        public float X1 { get => Get<float>(); set => Set(value); }
        public float X2 { get => Get<float>(); set => Set(value); }
        public float Y1 { get => Get<float>(); set => Set(value); }
        public float Y2 { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            return new RectangleF(X1, Y1, X2, Y2);
        }

        #endregion Methods
    }

    public abstract class Shape : Layer
    {
        #region Properties

        public override String DefaultName => "Shape";
        public BrushInfo FillBrush { get => Get<BrushInfo>(); set => Set(value); }
        public BrushInfo StrokeBrush { get => Get<BrushInfo>(); set => Set(value); }
        public StrokeStyleProperties StrokeStyle { get => Get<StrokeStyleProperties>(); set => Set(value); }
        public float StrokeWidth { get => Get<float>(); set => Set(value); }

        #endregion Properties
    }
}