using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Rain.Core.Utility {
    public static class ObservableHelper
    {
        public static IObservable<T> CreateObservable<T, TObserved>(
            this TObserved layer, Expression<Func<TObserved, T>> selector) where TObserved : INotifyPropertyChanged
        {
            var member = (MemberExpression) selector.Body;
            var name = member.Member.Name;

            var lambda = selector.Compile();

            return Observable
                  .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                       a => layer.PropertyChanged += a,
                       r => layer.PropertyChanged -= r)
                  .Where(args => args.PropertyName == name)
                  .Select(args => lambda(layer))
                  .DistinctUntilChanged();
        }

        public static IObservableProperty<T> Disposer<T>(this IObservable<T> observable) where T : IDisposable
        {
            return new ObservableDisposerProperty<T>(observable);
        }
    }
}