using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ibinimator.Service;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using SharpDX;
using SharpDX.Direct2D1;
using Point = System.Windows.Point;
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

        public float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            set => Set(value);
        }

        protected abstract string ElementName { get; }

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

        public override XElement GetElement()
        {
            var element = new XElement(ElementName);

            if (Name != null)
                element.Add(new XAttribute("id", Name));

            element.Add(new XAttribute("opacity", Opacity));

            return element;
        }
    }

    [XmlType("Color")]
    public class SolidColorBrushInfo : BrushInfo
    {
        public Color4 Color
        {
            get => Get<Color4>();
            set => Set(value);
        }

        protected override string ElementName => "solidColor";

        public override XElement GetElement()
        {
            var def = new XElement("solidColor");
            def.Add(new XAttribute("solid-color", Color.ToCss()));
            def.Add(new XAttribute("solid-opacity", Color.Alpha));
            return def;
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

        public ObservableCollection<GradientStop> Stops
        {
            get => Get<ObservableCollection<GradientStop>>();
            set => Set(value);
        }

        protected override string ElementName =>
            GradientType == GradientBrushType.Linear ? "linearGradient" : "radialGradient";

        public override XElement GetElement()
        {
            var def = base.GetElement();

            switch (GradientType)
            {
                case GradientBrushType.Linear:
                    def.Add(new XAttribute("x1", StartPoint.X));
                    def.Add(new XAttribute("y1", StartPoint.Y));
                    def.Add(new XAttribute("x2", EndPoint.X));
                    def.Add(new XAttribute("y2", EndPoint.Y));
                    break;
                case GradientBrushType.Radial:
                    def = new XElement("radialGradient");
                    def.Add(new XAttribute("cx", StartPoint.X));
                    def.Add(new XAttribute("cy", StartPoint.Y));
                    def.Add(new XAttribute("r", Vector2.Distance(StartPoint, EndPoint)));
                    def.Add(new XAttribute("fx", Focus.X));
                    def.Add(new XAttribute("fy", Focus.Y));
                    break;
            }

            foreach (var stop in Stops)
                def.Add(
                    new XElement("stop",
                        new XAttribute("offset", stop.Position),
                        new XAttribute("stop-color", ((Color4) stop.Color).ToCss()),
                        new XAttribute("stop-opacity", stop.Color.A)
                    ));

            def.Add(new XAttribute("gradientTransform", Transform.ToCss()));

            switch (ExtendMode)
            {
                case ExtendMode.Clamp:
                    def.Add(new XAttribute("spreadMethod", "pad"));
                    break;
                case ExtendMode.Wrap:
                    def.Add(new XAttribute("spreadMethod", "repeat"));
                    break;
                case ExtendMode.Mirror:
                    def.Add(new XAttribute("spreadMethod", "reflect"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return def;
        }

        public override Brush ToDirectX(RenderTarget target)
        {
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
                                ColorUtils.ToWpf(s.Color), s.Position))),
                        new Point(StartPoint.X, StartPoint.Y),
                        new Point(EndPoint.X, EndPoint.Y));
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

        protected override string ElementName => throw new NotImplementedException();

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