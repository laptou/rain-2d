﻿using SharpDX;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel
    {
        public class TransformViewModel : ViewModel
        {
            private readonly MainViewModel _parent;

            public TransformViewModel(MainViewModel parent)
            {
                _parent = parent;

                parent.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectionManager) && parent.SelectionManager != null)
                    {
                        parent.SelectionManager.Updated += (t, f) =>
                        {
                            RaisePropertyChanged(nameof(X));
                            RaisePropertyChanged(nameof(Y));
                            RaisePropertyChanged(nameof(Width));
                            RaisePropertyChanged(nameof(Height));
                            RaisePropertyChanged(nameof(Rotation));
                            RaisePropertyChanged(nameof(Shear));
                        };
                    }
                };
            }
            
            public float X
            {
                get => _parent.SelectionManager?.SelectionBounds.X ?? 0;
                set => _parent.SelectionManager?.Transform(
                    Vector2.One, 
                    new Vector2(value - X, 0), 
                    0,
                    0, 
                    Vector2.Zero);
            }

            public float Y
            {
                get => _parent.SelectionManager?.SelectionBounds.Y ?? 0;
                set => _parent.SelectionManager?.Transform(
                    Vector2.One,
                    new Vector2(0, value - Y),
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Width
            {
                get => _parent.SelectionManager?.SelectionBounds.Width ?? 0;
                set => _parent.SelectionManager?.Transform(
                    new Vector2(value / Width, 1),
                    Vector2.Zero,
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Height
            {
                get => _parent.SelectionManager?.SelectionBounds.Height ?? 0;
                set => _parent.SelectionManager?.Transform(
                    new Vector2(1, value / Height),
                    Vector2.Zero,
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Rotation
            {
                get => MathUtil.RadiansToDegrees(_parent.SelectionManager?.SelectionRotation ?? 0);
                set => _parent.SelectionManager?.Transform(
                    Vector2.One,
                    Vector2.Zero,
                    MathUtil.DegreesToRadians(value - Rotation),
                    0,
                    Vector2.Zero);
            }

            public float Shear
            {
                get => MathUtil.RadiansToDegrees(_parent.SelectionManager?.SelectionShear ?? 0);
                set => _parent.SelectionManager?.Transform(
                    Vector2.One,
                    Vector2.Zero,
                    0,
                    MathUtil.DegreesToRadians(value - Shear),
                    Vector2.Zero);
            }
        }
    }
}