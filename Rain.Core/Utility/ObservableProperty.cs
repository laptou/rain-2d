using System;

namespace Rain.Core.Utility {
    public class ObservableProperty<T> : IDisposable, IObservableProperty<T>
    {
        private IDisposable Subscription { get; }
        private IObservable<T> Source { get; }

        public T Value { get; private set; }

        public ObservableProperty(IObservable<T> source, T defaultValue = default)
        {
            Value = defaultValue;
            Source = source;
            Subscription = source.Subscribe(t => Value = t);
        }

        public void Dispose() => Subscription.Dispose();

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<T> observer) => Source.Subscribe(observer);
    }
}