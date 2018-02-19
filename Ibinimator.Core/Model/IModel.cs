using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public interface IModel : ICloneable, INotifyPropertyChanged, INotifyPropertyChanging
    {
        object Clone(Type type);
        T Clone<T>() where T : IModel;
        void SuppressNotifications();
        void RestoreNotifications();
    }
}