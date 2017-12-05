using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.ViewModel
{
    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        public NotifyTaskCompletion(Task<TResult> task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        public string ErrorMessage => InnerException?.Message;

        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsFaulted => Task.IsFaulted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : default;

        public TaskStatus Status => Task.Status;

        public Task<TResult> Task { get; }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }

            var propertyChanged = PropertyChanged;
            if (propertyChanged == null) return;
            propertyChanged(this, new PropertyChangedEventArgs("Status"));
            propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
            propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));
            if (task.IsCanceled)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
                propertyChanged(this, new PropertyChangedEventArgs("Exception"));
                propertyChanged(this,
                    new PropertyChangedEventArgs("InnerException"));
                propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
            }
            else
            {
                propertyChanged(this,
                    new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
                propertyChanged(this, new PropertyChangedEventArgs("Result"));
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public sealed class NotifyTaskCompletion : Model, IProgress<double>
    {
        public NotifyTaskCompletion(Task task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        public NotifyTaskCompletion(Func<IProgress<double>, Task> func)
        {
            Task = func(this);
            if (!Task.IsCompleted)
            {
                var _ = WatchTaskAsync(Task);
            }
        }

        public NotifyTaskCompletion(Func<Task> func)
        {
            Task = func();
            if (!Task.IsCompleted)
            {
                var _ = WatchTaskAsync(Task);
            }
        }

        public string ErrorMessage => InnerException?.Message;

        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsFaulted => Task.IsFaulted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsRunning => Task.Status == TaskStatus.Running;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public double Progress { get; private set; }

        public TaskStatus Status => Task.Status;

        public Task Task { get; }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }

            RaisePropertyChanged("Status");
            RaisePropertyChanged("IsCompleted");
            RaisePropertyChanged("IsNotCompleted");
            if (task.IsCanceled)
            {
                RaisePropertyChanged("IsCanceled");
            }
            else if (task.IsFaulted)
            {
                RaisePropertyChanged("IsFaulted");
                RaisePropertyChanged("Exception");
                RaisePropertyChanged("InnerException");
                RaisePropertyChanged("ErrorMessage");
            }
            else
            {
                RaisePropertyChanged("IsSuccessfullyCompleted");
                RaisePropertyChanged("Result");
            }
        }

        #region IProgress<double> Members

        public void Report(double value)
        {
            Progress = value;
            RaisePropertyChanged("Progress");
        }

        #endregion
    }
}