﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ibinimator.ViewModel
{
    public class DelegateCommand<T> : ICommand
    {
        private Action<T> _action;
        private Predicate<T> _predicate;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> action, Predicate<T> predicate)
        {
            _action = action;
            _predicate = predicate;
        }

        public DelegateCommand(Task action, Predicate<T> predicate)
        {
            _action = o => action.Start();
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

        public bool CanExecute(object parameter)
        {
            return _action != null && 
                _predicate?.Invoke(default(T) != null && parameter == null ? default(T) : (T)parameter) != false;
        }

        public void Execute(object parameter)
        {
            // casting null to a value type causes problems
           _action?.Invoke(default(T) != null && parameter == null ? default(T) : (T)parameter);
        }
    }

    public class AsyncDelegateCommand<T> : ICommand, INotifyPropertyChanged
    {
        private Func<T, Task> _task;

        public event EventHandler CanExecuteChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public AsyncDelegateCommand(Func<T, Task> task)
        {
            _task = task;
        }

        public AsyncDelegateCommand(Action<T> task) : 
            this(p => System.Threading.Tasks.Task.Run(() => task(p)))
        {
        }

        public Func<T, Task> Task
        {
            get => _task;
            set
            {
                if (_task != value && (_task == null || value == null))
                    CanExecuteChanged?.Invoke(this, null);

                _task = value;
            }
        }

        public NotifyTaskCompletion Execution { get; private set; }

        public bool CanExecute(object parameter)
        {
            return _task != null;
        }

        public void Execute(object parameter)
        {
            var castedParameter = default(T) != null && parameter == null ? default(T) : (T) parameter;
            Execution = new NotifyTaskCompletion(_task?.Invoke(castedParameter));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Execution"));
        }
    }
}