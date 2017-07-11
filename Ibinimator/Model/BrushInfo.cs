using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;

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
                Stops = new List<GradientStop>();
        }

        #endregion Constructors

        #region Properties

        public Vector2 StartPoint
        {
            get => Get<Vector2>();
            set { if (IsGradient) Set(value); else throw new InvalidOperationException("Not a gradient."); }
        }

        public byte[] Bitmap { get; set; }

        public BrushType BrushType { get => Get<BrushType>(); private set => Set(value); }

        public Matrix3x2 Transform { get => Get<RawMatrix3x2>(); set => Set(value); }

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
}