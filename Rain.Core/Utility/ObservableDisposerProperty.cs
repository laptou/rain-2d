using System;

namespace Rain.Core.Utility {
    public class ObservableDisposerProperty<T> : IDisposable, IObservableProperty<T> where T : IDisposable
    {
        private IDisposable Subscription { get; }
        private IObservable<T> Source { get; }

        public T Value { get; private set; }

        public ObservableDisposerProperty(IObservable<T> source, T defaultValue = default)
        {
            Value = defaultValue;
            Source = source;
            Subscription = source.Subscribe(t =>
                                            {
                                                var old = Value;
                                                Value = t;
                                                old?.Dispose();
                                            });
        }

        public void Dispose() => Subscription.Dispose();

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<T> observer) => Source.Subscribe(observer);
    }
}