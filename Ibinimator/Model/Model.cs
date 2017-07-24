using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ibinimator.Model
{
    public abstract class Model : INotifyPropertyChanged,
        INotifyPropertyChanging
    {
        #region Fields

        private Dictionary<string, Object> properties = new Dictionary<string, object>();

        #endregion Fields

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        #endregion Events

        #region Methods

        public T Get<T>([CallerMemberName] string propertyName = "")
        {
            return properties.TryGetValue(propertyName, out object o) && o is T ? (T)o : default(T);
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;
            RaisePropertyChanged(propertyName);
        }

        public void RaisePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(sender, args);
        }

        public void RaisePropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        public void RaisePropertyChanging<T>(Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);
        }

        public void Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            RaisePropertyChanging(propertyName);
            properties[propertyName] = value;
            RaisePropertyChanged(propertyName);
        }

        public void Set<T>(T value, ref T variable, [CallerMemberName] string propertyName = "")
        {
            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

        public void SetProperty<T>(T value, ref T variable, Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

        private PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TProperty>> propertyLambda)
        {
            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            return propInfo;
        }

        #endregion Methods
    }
}