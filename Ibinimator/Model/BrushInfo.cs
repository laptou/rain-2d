using System;
using System.Collections.Generic;
using Ibinimator.Service;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ibinimator.Utility;
using SharpDX;
using SharpDX.Direct2D1;
using WPF = System.Windows.Media;

namespace Ibinimator.Model
{
    public enum GradientBrushType
    {
        Linear,
        Radial
    }

    [XmlInclude(typeof(SolidColorBrushInfo))]
    [XmlInclude(typeof(GradientBrushInfo))]
    [XmlInclude(typeof(BitmapBrushInfo))]
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

        public abstract Brush ToDirectX(RenderTarget target);

        public abstract WPF.Brush ToWpf();

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

    [XmlType("Color")]
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

        public override string GetReference()
        {
            return Scope == ResoureScope.Local ? Color.ToCss() : base.GetReference();
        }

        public override Brush ToDirectX(RenderTarget target)
        {
            return new SolidColorBrush(
                target,
                Color,
                new BrushProperties {Opacity = Opacity, Transform = Transform});
        }

        public override string ToString()
        {
            return $"Color: {Color}, Opacity: {Opacity}";
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

        public override Brush ToDirectX(RenderTarget target)
        {
            if (Stops.Count == 0) return null;

            switch (GradientType)
            {
                case GradientBrushType.Linear:
                    return new LinearGradientBrush(
                        target,
                        new LinearGradientBrushProperties
                        {
                            EndPoint = EndPoint,
                            StartPoint = StartPoint
                        },
                        new BrushProperties {Opacity = Opacity, Transform = Transform},
                        new GradientStopCollection(target, Stops.ToArray()));
                case GradientBrushType.Radial:
                    return new RadialGradientBrush(
                        target,
                        new RadialGradientBrushProperties
                        {
                            Center = StartPoint,
                            RadiusX = EndPoint.X - StartPoint.X,
                            RadiusY = EndPoint.Y - StartPoint.Y,
                            GradientOriginOffset = Focus - StartPoint
                        },
                        new BrushProperties {Opacity = Opacity, Transform = Transform},
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
                                ColorUtils.ToWpf(s.Color), s.Position))));
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