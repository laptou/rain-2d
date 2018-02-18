using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public abstract class Model : IModel
    {
        private readonly Dictionary<string, Delegate>
            _handlers = new Dictionary<string, Delegate>();

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected T Get<T>([CallerMemberName] string propertyName = "")
        {
            return _properties.TryGetValue(propertyName, out var o) && o is T ? (T) o : default;
        }

        protected PropertyInfo GetPropertyInfo<TProperty>(
            Expression<Func<TProperty>> propertyLambda)
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

        protected void RaisePropertyChanged(params string[] propertyNames)
        {
            try
            {
                foreach (var propertyName in propertyNames)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                #if DEBUG
                Debugger.Log(3, "Error-PropertyChanged-Handler", ex.Message);
                #endif
            }
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
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected void RaisePropertyChanging<T>(Expression<Func<T>> propertyLambda)
        {
            var propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);
        }

        protected void Set<T>(
            T value, [CallerMemberName] string propertyName = "",
            params string[] dependentProperties)
        {
            if (value is INotifyCollectionChanged collection)
            {
                var old = Get<INotifyCollectionChanged>(propertyName);

                if (old != null &&
                    _handlers.ContainsKey(propertyName))
                    old.CollectionChanged -=
                        (NotifyCollectionChangedEventHandler) _handlers[propertyName];

                _handlers[propertyName] =
                    new NotifyCollectionChangedEventHandler((s, e) =>
                                                            {
                                                                RaisePropertyChanged(propertyName);
                                                            });

                collection.CollectionChanged +=
                    (NotifyCollectionChangedEventHandler) _handlers[propertyName];
            }

            RaisePropertyChanging(propertyName);
            _properties[propertyName] = value;
            RaisePropertyChanged(propertyName);

            foreach (var property in dependentProperties)
                RaisePropertyChanged(property);
        }

        protected void Set<T>(
            T value, PropertyChangedEventHandler after, [CallerMemberName] string propertyName = "")
        {
            Set(value, propertyName);
            after?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T>(
            T value, out T variable, PropertyChangedEventHandler after,
            [CallerMemberName] string propertyName = "")
        {
            Set(value, out variable, propertyName);
            after?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T>(
            T value, PropertyChangingEventHandler before, PropertyChangedEventHandler after,
            [CallerMemberName] string propertyName = "")
        {
            before?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            Set(value, after, propertyName);
        }

        protected void Set<T>(T value, out T variable, [CallerMemberName] string propertyName = "")
        {
            // if (Equals(variable, value)) return;

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

        protected void SilentSet<T>(T value, [CallerMemberName] string propertyName = "")
        {
            _properties[propertyName] = value;
        }

        #region IModel Members

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        public T Clone<T>() where T : IModel { return (T) Clone(GetType()); }

        public object Clone(Type type)
        {
            var t = (Model) Activator.CreateInstance(type);
            t._properties =
                _properties.ToDictionary(kv => kv.Key,
                                         kv => (kv.Value as Model)?.Clone() ?? kv.Value);

            return t;
        }

        public object Clone() { return Clone(GetType()); }

        #endregion
    }
}