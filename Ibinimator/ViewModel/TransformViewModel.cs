using Ibinimator.View.Control;
using SharpDX;
using System;
using System.ComponentModel;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel
    {
        public class TransformViewModel : ViewModel
        {
            private MainViewModel parent;

            public TransformViewModel(MainViewModel parent)
            {
                this.parent = parent;

                parent.PropertyChanged += (s, e) =>
                {
                    if(e.PropertyName == nameof(SelectionManager) && parent.SelectionManager != null)
                        parent.SelectionManager.PropertyChanged += SelectionManagerPropertyChanged;
                };
            }

            private void SelectionManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(ISelectionManager.SelectionBounds):
                        RaisePropertyChanged(nameof(X));
                        RaisePropertyChanged(nameof(Y));
                        RaisePropertyChanged(nameof(Width));
                        RaisePropertyChanged(nameof(Height));
                        break;

                    case nameof(ISelectionManager.SelectionRotation):
                        RaisePropertyChanged(nameof(Rotation));
                        break;

                    case nameof(ISelectionManager.SelectionShear):
                        RaisePropertyChanged(nameof(Shear));
                        break;
                }
            }

            public float X
            {
                get => parent.SelectionManager?.SelectionBounds.X ?? 0;
                set => parent.SelectionManager?.Transform(
                    Vector2.One, 
                    new Vector2(value - X, 0), 
                    0,
                    0, 
                    Vector2.Zero);
            }

            public float Y
            {
                get => parent.SelectionManager?.SelectionBounds.Y ?? 0;
                set => parent.SelectionManager?.Transform(
                    Vector2.One,
                    new Vector2(0, value - Y),
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Width
            {
                get => parent.SelectionManager?.SelectionBounds.Width ?? 0;
                set => parent.SelectionManager?.Transform(
                    new Vector2(value / Width, 1),
                    Vector2.Zero,
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Height
            {
                get => parent.SelectionManager?.SelectionBounds.Height ?? 0;
                set => parent.SelectionManager?.Transform(
                    new Vector2(1, value / Height),
                    Vector2.Zero,
                    0,
                    0,
                    Vector2.Zero);
            }

            public float Rotation
            {
                get => parent.SelectionManager?.SelectionRotation ?? 0;
                set => parent.SelectionManager?.Transform(
                    Vector2.One,
                    Vector2.Zero,
                    value - Rotation,
                    0,
                    Vector2.Zero);
            }

            public float Shear
            {
                get => parent.SelectionManager?.SelectionShear ?? 0;
                set => parent.SelectionManager?.Transform(
                    Vector2.One,
                    Vector2.Zero,
                    0,
                    value - Shear,
                    Vector2.Zero);
            }
        }
    }
}