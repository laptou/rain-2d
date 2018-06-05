using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Rain.Core.Utility
{
    public static class ObservableHelper
    {
        public static IObservable<T>
            CreateObservable<T, TObserved>(this TObserved layer, string property, Func<TObserved, T> selector)
            where TObserved : INotifyPropertyChanged
        {
            return Observable
                  .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs
                       >(a => layer.PropertyChanged += a, r => layer.PropertyChanged -= r)
                  .Where(p => p.EventArgs.PropertyName == property)
                  .Select(args => selector(layer))
                  .StartWith(selector(layer))
                  .DistinctUntilChanged();
        }

        public static IObservable<T> CreateObservable<T, TObserved>(
            this TObserved layer, string property, Func<TObserved, T> selector, IEqualityComparer<T> comparer)
            where TObserved : INotifyPropertyChanged
        {
            return Observable
                  .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs
                       >(a => layer.PropertyChanged += a, r => layer.PropertyChanged -= r)
                  .Where(p => p.EventArgs.PropertyName == property)
                  .Select(args => selector(layer))
                  .StartWith(selector(layer))
                  .DistinctUntilChanged(comparer);
        }

        public static IObservable<T> CreateObservable<T, TObserved, TKey>(
            this TObserved layer, string property, Func<TObserved, T> selector, Func<T, TKey> keySelector)
            where TObserved : INotifyPropertyChanged
        {
            return Observable
                  .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs
                       >(a => layer.PropertyChanged += a, r => layer.PropertyChanged -= r)
                  .Where(p => p.EventArgs.PropertyName == property)
                  .Select(args => selector(layer))
                  .StartWith(selector(layer))
                  .DistinctUntilChanged(keySelector);
        }
    }
}