using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core;
using Rain.Renderer;

namespace Rain.ViewModel
{
    public class TransformViewModel : ViewModel
    {
        private IObservable<EventPattern<object>> _attachObservable;
        private IObservable<EventPattern<object>> _detachObservable;

        public TransformViewModel(IArtContext artContext)
        {
            ArtContext = artContext;

            _attachObservable = Observable.FromEventPattern(h => ArtContext.ManagerAttached += h,
                                                            h => ArtContext.ManagerAttached -= h);
            _detachObservable = Observable.FromEventPattern(h => ArtContext.ManagerDetached += h,
                                                            h => ArtContext.ManagerDetached -= h);

            _attachObservable.Subscribe(ArtContextOnManagerAttached);

            if (artContext.SelectionManager != null)
                ArtContextOnManagerAttached(new EventPattern<object>(artContext.SelectionManager, null));
        }

        public IArtContext ArtContext { get; }

        public float Height
        {
            get => GetSize().Y;
            set => SetSize(new Vector2(Width, value));
        }

        public SelectionHandle Origin
        {
            get => Get<SelectionHandle>();
            set => Set(value);
        }

        public float Rotation
        {
            get => ArtContext.SelectionManager?.SelectionTransform.GetRotation() ?? 0;
            set => ArtContext.SelectionManager?.TransformSelection(
                Vector2.One,
                Vector2.Zero,
                value - Rotation,
                0,
                Vector2.One * 0.5f);
        }

        public float Shear
        {
            get => ArtContext.SelectionManager?.SelectionTransform.GetShear() ?? 0;
            set => ArtContext.SelectionManager?.TransformSelection(
                Vector2.One,
                Vector2.Zero,
                0,
                value - Shear,
                Vector2.Zero);
        }

        public float Width
        {
            get => GetSize().X;
            set => SetSize(new Vector2(value, Height));
        }

        public float X
        {
            get => GetPosition().X;
            set => SetPosition(new Vector2(value, Y));
        }

        public float Y
        {
            get => GetPosition().Y;
            set => SetPosition(new Vector2(X, value));
        }

        private void ArtContextOnManagerAttached(EventPattern<object> evt)
        {
            if (evt.Sender is ISelectionManager sm)
            {
                Observable.FromEventPattern(h =>
                                            {
                                                sm.SelectionChanged += h;
                                                sm.SelectionBoundsChanged += h;
                                            },
                                            h =>
                                            {
                                                sm.SelectionChanged -= h;
                                                sm.SelectionBoundsChanged -= h;
                                            })
                          .Sample(TimeSpan.FromMilliseconds(250))
                          .TakeUntil(_detachObservable.Where(e => e.Sender == sm))
                          .SubscribeOn(App.Dispatcher)
                          .Subscribe(SelectionManagerSelectionChanged);
            }
        }

        private Vector2 GetPosition()
        {
            return Vector2.Transform(ArtContext.SelectionManager.SelectionBounds.TopLeft,
                                     ArtContext.SelectionManager.SelectionTransform);
        }

        private Vector2 GetSize()
        {
            if (ArtContext.SelectionManager == null)
                return Vector2.Zero;

            return ArtContext.SelectionManager.SelectionBounds.Size *
                   ArtContext.SelectionManager.SelectionTransform.GetScale();
        }

        private void SelectionManagerSelectionChanged(EventPattern<object> evt)
        {
            RaisePropertyChanged(nameof(X),
                                 nameof(Y),
                                 nameof(Width),
                                 nameof(Height),
                                 nameof(Rotation),
                                 nameof(Shear));
        }

        private void SetPosition(Vector2 position)
        {
            ArtContext.SelectionManager?.TransformSelection(Vector2.One,
                                                            position - GetPosition(),
                                                            0,
                                                            0,
                                                            Vector2.Zero);
        }

        private void SetSize(Vector2 size)
        {
            ArtContext.SelectionManager?.TransformSelection(size / GetSize(),
                                                            Vector2.Zero,
                                                            0,
                                                            0,
                                                            Vector2.Zero);
        }
    }
}