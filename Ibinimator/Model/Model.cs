using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public abstract class Model : INotifyPropertyChanged,
        INotifyPropertyChanging
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();
        private Dictionary<string, Delegate> _handlers = new Dictionary<string, Delegate>();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        #endregion

        public T Clone<T>() where T : Model, new()
        {
            if (typeof(T) != GetType())
                throw new InvalidCastException();

            return new T
            {
                _properties = _properties.ToDictionary(kv => kv.Key, kv => kv.Value)
            };
        }

        protected T Get<T>([CallerMemberName] string propertyName = "")
        {
            return _properties.TryGetValue(propertyName, out object o) && o is T ? (T) o : default(T);
        }

        protected PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TProperty>> propertyLambda)
        {
            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a field, not a property.");

            return propInfo;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyLambda)
        {
            var propertyName = GetPropertyInfo(propertyLambda).Name;
            RaisePropertyChanged(propertyName);
        }

        protected void RaisePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(sender, args);
        }

        protected void RaisePropertyChanging(object sender, PropertyChangingEventArgs args)
        {
            PropertyChanging?.Invoke(sender, args);
        }

        protected void RaisePropertyChanging(string propertyName)
        {
            RaisePropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        protected void RaisePropertyChanging<T>(Expression<Func<T>> propertyLambda)
        {
            var propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(Get<T>(propertyName), value)) return;

            if (value is INotifyCollectionChanged collection)
            {
                var old = Get<INotifyCollectionChanged>(propertyName);

                if (old != null && _handlers.ContainsKey(propertyName))
                    old.CollectionChanged -= (NotifyCollectionChangedEventHandler)_handlers[propertyName];

                _handlers[propertyName] = new NotifyCollectionChangedEventHandler((s, e) =>
                {
                    RaisePropertyChanged(propertyName);
                });

                collection.CollectionChanged += (NotifyCollectionChangedEventHandler)_handlers[propertyName];
            }

            RaisePropertyChanging(propertyName);
            _properties[propertyName] = value;
            RaisePropertyChanged(propertyName);
        }

        protected void Set<T>(T value, ref T variable, [CallerMemberName] string propertyName = "")
        {
            if (Equals(variable, value)) return;

            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

        protected void SetProperty<T>(T value, out T variable, Expression<Func<T>> propertyLambda)
        {
            var propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

    }
}