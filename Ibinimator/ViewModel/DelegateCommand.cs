using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            return _action != null && Predicate?.Invoke((T)parameter) != false;
        }

        public void Execute(object parameter)
        {
            try
            {
                _action?.Invoke((T)parameter);
            }
            finally
            {
                
            }
        }
    }

    public class AsyncDelegateCommand<T> : ICommand, INotifyPropertyChanged
    {
        private Func<Task> _task;

        public event EventHandler CanExecuteChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public AsyncDelegateCommand(Func<Task> task)
        {
            _task = task;
        }

        public AsyncDelegateCommand(Action task) : this(() => System.Threading.Tasks.Task.Run(task))
        {
        }

        public Func<Task> Task
        {
            get
            {
                return _task;
            }
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
            Execution = new NotifyTaskCompletion(_task?.Invoke());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Execution"));
        }
    }
}