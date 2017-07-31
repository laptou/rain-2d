using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ibinimator.ViewModel
{
    public class DelegateCommand : ICommand
    {
        private Action<object> _action;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> action)
        {
            this._action = action;
        }

        public DelegateCommand(Task action)
        {
            this._action = (o) => action.Start();
        }

        public Action<object> Action
        {
            get
            {
                return _action;
            }
            set
            {
                if (_action != value && (_action == null || value == null))
                    CanExecuteChanged?.Invoke(this, null);

                _action = value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return _action != null;
        }

        public void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }
    }

    public class AsyncDelegateCommand : ICommand, INotifyPropertyChanged
    {
        private Func<Task> _task;

        public event EventHandler CanExecuteChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public AsyncDelegateCommand(Func<Task> task)
        {
            this._task = task;
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