using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Rain.ViewModel
{
    public class BindableDelegateCommand : DependencyObject, ICommand
    {
        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.Register("Action",
                                        typeof(Action),
                                        typeof(BindableDelegateCommand),
                                        new PropertyMetadata(default(Action)));

        public Action Action
        {
            get => (Action) GetValue(ActionProperty);
            set => SetValue(ActionProperty, value);
        }

        #region ICommand Members

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => Action?.Invoke();

        #endregion
    }

    public class DelegateCommand<T> : Core.Model.Model, ICommand
    {
        private Action<T>    _action;
        private Predicate<T> _predicate;

        public DelegateCommand(Action<T> action, Predicate<T> predicate)
        {
            _action = action;
            _predicate = predicate;
        }

        public Action<T> Action
        {
            get => _action;
            set
            {
                if (_action != value)
                    CanExecuteChanged?.Invoke(this, null);

                _action = value;
            }
        }

        public Exception Exception
        {
            get => Get<Exception>();
            private set => Set(value);
        }

        public Predicate<T> Predicate
        {
            get => _predicate;
            set
            {
                if (_predicate != value)
                    CanExecuteChanged?.Invoke(this, null);

                _predicate = value;
            }
        }

        #region ICommand Members

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _action != null &&
                   _predicate?.Invoke(default(T) != null && parameter == null
                                          ? default
                                          : (T) parameter) != false;
        }

        public void Execute(object parameter)
        {
            try
            {
                // casting null to a value type causes problems
                _action?.Invoke(default(T) != null && parameter == null ? default : (T) parameter);
            }
            catch (Exception e)
            {
                Exception = e;
            }
        }

        #endregion
    }

    public class AsyncDelegateCommand<T> : ICommand, INotifyPropertyChanged
    {
        private Func<T, Task> _task;

        public AsyncDelegateCommand(Func<T, Task> task) { _task = task; }

        public AsyncDelegateCommand(Action<T> task) : this(
            p => System.Threading.Tasks.Task.Run(() => task(p))) { }

        public NotifyTaskCompletion Execution { get; private set; }

        public Func<T, Task> Task
        {
            get => _task;
            set
            {
                if (_task != value &&
                    (_task == null || value == null))
                    CanExecuteChanged?.Invoke(this, null);

                _task = value;
            }
        }

        public event EventHandler Executed;

        #region ICommand Members

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) { return _task != null; }

        public void Execute(object parameter)
        {
            var castedParameter = default(T) != null && parameter == null ? default : (T) parameter;
            Execution = new NotifyTaskCompletion(_task?.Invoke(castedParameter));
            Executed?.Invoke(this, null);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Execution"));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}