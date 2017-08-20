using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using WPF = System.Windows.Media;

namespace Ibinimator.Model
{
    public enum GradientBrushType
    {
        Linear, Radial
    }

    [XmlInclude(typeof(SolidColorBrushInfo))]
    [XmlInclude(typeof(GradientBrushInfo))]
    [XmlInclude(typeof(BitmapBrushInfo))]
    public abstract class BrushInfo : Model
    {
        #region Constructors

        protected BrushInfo()
        {
            Opacity = 1;
            Transform = Matrix3x2.Identity;
        }

        #endregion Constructors

        #region Properties

        [XmlAttribute]
        public float Opacity { get => Get<float>(); set => Set(value); }
        
        public Matrix3x2 Transform { get => Get<Matrix3x2>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public abstract Brush ToDirectX(RenderTarget target);

        public abstract WPF.Brush ToWpf();

        public virtual void Copy(BrushInfo brush)
        {
            if (this.GetType() != brush.GetType())
                throw new InvalidOperationException();

            Opacity = brush.Opacity;
            Transform = brush.Transform;
        }

        #endregion Methods
    }

    [XmlType("Color")]
    public class SolidColorBrushInfo : BrushInfo
    {
        public Color4 Color
        {
            get => Get<Color4>();
            set => Set(value);
        }

        public override Brush ToDirectX(RenderTarget target)
        {
            return new SolidColorBrush(
                target,
                Color,
                new BrushProperties { Opacity = Opacity, Transform = Transform });
        }

        public override WPF.Brush ToWpf()
        {
            return new WPF.SolidColorBrush(Color.ToWpf());
        }
    }

    [XmlType("Gradient")]
    public class GradientBrushInfo : BrushInfo
    {
        public GradientBrushInfo()
        {
            Stops = new ObservableCollection<GradientStop>();
        }

        public Vector2 StartPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public ObservableCollection<GradientStop> Stops
        {
            get => Get<ObservableCollection<GradientStop>>();
            set => Set(value);
        }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
            set => Set(value);
        }

        public Vector2 EndPoint
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public GradientBrushType GradientType
        {
            get => Get<GradientBrushType>();
            set => Set(value);
        }

        public override Brush ToDirectX(RenderTarget target)
        {
            switch (GradientType)
            {
                case GradientBrushType.Linear:
                    return new LinearGradientBrush(
                        target,
                        new LinearGradientBrushProperties { EndPoint = EndPoint, StartPoint = StartPoint },
                        new BrushProperties { Opacity = Opacity, Transform = Transform },
                        new GradientStopCollection(target, Stops.ToArray()));
                case GradientBrushType.Radial:
                    return new RadialGradientBrush(
                        target,
                        new RadialGradientBrushProperties
                        {
                            Center = StartPoint,
                            RadiusX = EndPoint.X - StartPoint.X,
                            RadiusY = EndPoint.Y - StartPoint.Y
                        },
                        new BrushProperties { Opacity = Opacity, Transform = Transform },
                        new GradientStopCollection(target, Stops.ToArray()));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override WPF.Brush ToWpf()
        {
            switch (GradientType)
            {
                case GradientBrushType.Linear:
                    return new WPF.LinearGradientBrush(
                        new WPF.GradientStopCollection(
                            Stops.Select(s => new WPF.GradientStop(
                                ColorUtils.ToWpf(s.Color), s.Position))), 
                        new System.Windows.Point(StartPoint.X, StartPoint.Y),
                        new System.Windows.Point(EndPoint.X, EndPoint.Y));
                case GradientBrushType.Radial:
                    return new WPF.RadialGradientBrush(
                        new WPF.GradientStopCollection(
                            Stops.Select(s => new WPF.GradientStop(
                                ColorUtils.ToWpf(s.Color), s.Position))));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [XmlType("Bitmap")]
    public class BitmapBrushInfo : BrushInfo
    {
        public byte[] Bitmap { get; set; }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
            set => Set(value);
        }

        public override Brush ToDirectX(RenderTarget target)
        {
            throw new NotImplementedException();
        }

        public override WPF.Brush ToWpf()
        {
            throw new NotImplementedException();
        }
    }
}