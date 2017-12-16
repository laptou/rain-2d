using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
{
    public class CommandManager : Core.Model.Model
    {
        public static CommandManager Instance = new CommandManager();

        private CommandManager()
        {
        }

        public Exception LastError
        {
            get => Get<Exception>();
            set => Set(value);
        }

        public static DelegateCommand<T> Register<T>(Action<T> action)
        {
            var command = new DelegateCommand<T>(action, null);
            command.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(DelegateCommand<T>.Exception))
                    Instance.LastError = command.Exception;
            };
            return command;
        }

        public static AsyncDelegateCommand<T> RegisterAsync<T>(Func<T, Task> task)
        {
            var command = new AsyncDelegateCommand<T>(task);
            command.PropertyChanged += (sender, args) =>
            {
                command.Execution.PropertyChanged += (s, a) =>
                {
                    if (a.PropertyName == nameof(NotifyTaskCompletion.Exception))
                        Instance.LastError = command.Execution.Exception;
                };

                if (args.PropertyName == nameof(AsyncDelegateCommand<T>.Execution))
                    Instance.LastError = command.Execution.Exception;
            };
            return command;
        }
    }
}