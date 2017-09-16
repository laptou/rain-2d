using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public class CommandManager : Model.Model
    {
        public static CommandManager Instance = new CommandManager();

        public DelegateCommand<T> Register<T>(Action<T> action)
        {
            var command = new DelegateCommand<T>(action, null);
            command.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(DelegateCommand<T>.Exception))
                    LastError = command.Exception;
            };
            return command;
        }

        public AsyncDelegateCommand<T> RegisterAsync<T>(Func<T, Task> task)
        {
            var command = new AsyncDelegateCommand<T>(task);
            command.PropertyChanged += (sender, args) =>
            {
                command.Execution.PropertyChanged += (s, a) =>
                {
                    if (a.PropertyName == nameof(NotifyTaskCompletion.Exception))
                        LastError = command.Execution.Exception;
                };

                if (args.PropertyName == nameof(AsyncDelegateCommand<T>.Execution))
                    LastError = command.Execution.Exception;
            };
            return command;
        }

        public Exception LastError
        {
            get => Get<Exception>();
            set => Set(value);
        }
    }
}
