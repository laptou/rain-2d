using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WPF = System.Windows.Media;

namespace Ibinimator.Model
{
    public enum BrushType
    {
        Color, Bitmap, Image, LinearGradient, RadialGradient
    }

    public class BrushInfo : Model
    {
        #region Constructors

        public BrushInfo(BrushType brushType)
        {
            BrushType = brushType;

            Opacity = 1;

            if (IsGradient)
                Stops = new ObservableCollection<GradientStop>();
        }

        #endregion Constructors

        #region Properties

        public byte[] Bitmap { get; set; }

        public BrushType BrushType { get => Get<BrushType>(); private set => Set(value); }

        public Color4 Color
        {
            get => Get<Color4>();
            set { if (!IsGradient && !IsImage) Set(value); else throw new InvalidOperationException("Not a color brush."); }
        }

        public Vector2 EndPoint
        {
            get => Get<Vector2>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public ExtendMode ExtendMode
        {
            get => Get<ExtendMode>();
            set
            {
                if (IsGradient || IsImage) Set(value);
                else throw new InvalidOperationException("Not valid for this type of brush.");
            }
        }

        public float Opacity { get => Get<float>(); set => Set(value); }

        public Vector2 StartPoint
        {
            get => Get<Vector2>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public ObservableCollection<GradientStop> Stops
        {
            get => Get<ObservableCollection<GradientStop>>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public Matrix3x2 Transform { get => Get<Matrix3x2>(); set => Set(value); }

        private bool IsGradient => BrushType == BrushType.LinearGradient || BrushType == BrushType.RadialGradient;

        private bool IsImage => BrushType == BrushType.Image || BrushType == BrushType.Bitmap;

        #endregion Properties

        #region Methods

        public Brush ToDirectX(RenderTarget target)
        {
            var props = new BrushProperties() { Opacity = Opacity, Transform = Transform };

            switch (BrushType)
            {
                case BrushType.Color:
                    return new SolidColorBrush(target, Color, props);

                case BrushType.LinearGradient:
                    return new LinearGradientBrush(
                        target,
                        new LinearGradientBrushProperties() { EndPoint = EndPoint, StartPoint = StartPoint },
                        props,
                        new GradientStopCollection(target, Stops.ToArray()));

                case BrushType.RadialGradient:
                    return new RadialGradientBrush(
                        target,
                        new RadialGradientBrushProperties()
                        {
                            Center = StartPoint,
                            RadiusX = EndPoint.X - StartPoint.X,
                            RadiusY = EndPoint.Y - StartPoint.Y
                        },
                        props,
                        new GradientStopCollection(target, Stops.ToArray()));

                default:
                    throw new NotImplementedException();
            }
        }

        public WPF.Brush ToWPF()
        {
            return App.Current.Dispatcher.Invoke<WPF.Brush>(() =>
            {
                switch (BrushType)
                {
                    case BrushType.Color:
                        return new WPF.SolidColorBrush(Color.ToWPF());

                    case BrushType.LinearGradient:
                        return new WPF.LinearGradientBrush(
                            new WPF.GradientStopCollection(
                                Stops.Select(stop => new WPF.GradientStop(((Color4)stop.Color).ToWPF(), stop.Position))
                            ));

                    case BrushType.RadialGradient:
                        return new WPF.RadialGradientBrush(
                            new WPF.GradientStopCollection(
                                Stops.Select(stop => new WPF.GradientStop(((Color4)stop.Color).ToWPF(), stop.Position))
                            ));

                    default:
                        throw new NotImplementedException();
                }
            });
        }

        public void Copy(BrushInfo brush)
        {
            if (this.BrushType != brush?.BrushType)
                throw new InvalidOperationException();

            Opacity = brush.Opacity;
            Transform = brush.Transform;

            switch (BrushType)
            {
                case BrushType.Color:
                    Color = brush.Color;
                    break;
                case BrushType.Bitmap:
                    Bitmap = brush.Bitmap;
                    ExtendMode = brush.ExtendMode;
                    break;
                case BrushType.Image:
                    throw new NotImplementedException();
                case BrushType.LinearGradient:
                case BrushType.RadialGradient:
                    EndPoint = brush.EndPoint;
                    StartPoint = brush.StartPoint;
                    Stops = brush.Stops;
                    ExtendMode = brush.ExtendMode;
                    break;
                default:
                    break;
            }

        }

        #endregion Methods
    }
}