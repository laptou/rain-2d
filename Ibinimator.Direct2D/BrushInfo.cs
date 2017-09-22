using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ibinimator.Core;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Direct2D
{
    public enum GradientBrushType
    {
        Linear,
        Radial
    }
    
    public abstract class BrushInfo : Resource
    {
        private static long _nextId = 1;

        protected BrushInfo()
        {
            Opacity = 1;
            Transform = Matrix3x2.Identity;
            Name = _nextId++.ToString();
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public virtual float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            set => Set(value);
        }

        public virtual void Copy(BrushInfo brush)
        {
            if (GetType() != brush.GetType())
                throw new InvalidOperationException();

            Opacity = brush.Opacity;
            Transform = brush.Transform;
        }

        public virtual string GetReference()
        {
            switch (Scope)
            {
                case ResoureScope.Local:
                    throw new NotImplementedException();
                case ResoureScope.Document:
                    return $"url(#{Name})";
                case ResoureScope.Application:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class SolidColorBrushInfo : BrushInfo
    {
        public SolidColorBrushInfo()
        {
        }

        public SolidColorBrushInfo(Color4 color)
        {
            Color = color;
        }

        public Color4 Color
        {
            get => Get<Color4>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(Opacity));
            }
        }

        public override float Opacity
        {
            get => Color.Alpha;
            set
            {
                var color = Color;
                color.Alpha = value;
                Color = color;
            }
        }

        public override string ToString()
        {
            return $"Color: {Color}, Opacity: {Opacity}";
        }
    }

    public class GradientBrushInfo : BrushInfo
    {
        public GradientBrushInfo()
        {
            Stops = new ObservableList<GradientStop>();
        }

        public Vector2 EndPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
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
    }

    public class BitmapBrushInfo : BrushInfo
    {
        public byte[] Bitmap { get; set; }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
            set => Set(value);
        }
    }
}