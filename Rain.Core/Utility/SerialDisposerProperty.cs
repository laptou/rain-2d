using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;

namespace Rain.Core.Utility
{
    public class SerialDisposerProperty<T> : PropertyChangedBase, ISerialProperty<T> where T : IDisposable
    {
        private readonly SerialDisposable _serialDisposable;
        private readonly IDisposable      _subscription;

        public SerialDisposerProperty(IObservable<T> source)
        {
            _serialDisposable = new SerialDisposable();
            _subscription = source.Subscribe(item => Value = item);
        }

        #region IObservableProperty<T> Members

        public void Dispose()
        {
            _serialDisposable?.Dispose();
            _subscription?.Dispose();
        }

        public T Value
        {
            get => _serialDisposable.Disposable is T value ? value : default;
            private set
            {
                _serialDisposable.Disposable = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        #endregion
    }

    public class SerialProperty<T> : PropertyChangedBase, ISerialProperty<T> 
    {
        private readonly IDisposable _subscription;
        private          T           _value;

        public SerialProperty(IObservable<T> source) { _subscription = source.Subscribe(item => Value = item); }

        #region IObservableProperty<T> Members

        public void Dispose() { _subscription?.Dispose(); }

        public T Value
        {
            get => _value;
            private set
            {
                _value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        #endregion
    }

    public class SerialDisposerObservable<T> : IObservable<T> where T : IDisposable
    {
        private readonly SerialDisposable _serialDisposable;
        private readonly IObservable<T>   _source;

        public SerialDisposerObservable(IObservable<T> source)
        {
            _serialDisposable = new SerialDisposable();
            _source = source.Do(item => _serialDisposable.Disposable = item);
        }

        public void Dispose() { _serialDisposable?.Dispose(); }

        #region IObservable<T> Members

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<T> observer) { return _source.Subscribe(observer); }

        #endregion
    }
}