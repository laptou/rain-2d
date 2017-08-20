using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace Ibinimator.ViewModel
{
    public abstract class ViewModel : Model.Model
    {
        #region Events

        public event Action CloseRequested;

        public event Action HideRequested;

        public event Action ShowRequested;

        #endregion Events

        #region Methods

        public void CloseView()
        {
            CloseRequested?.Invoke();
        }

        public void HideView()
        {
            HideRequested?.Invoke();
        }

        public void ShowView()
        {
            ShowRequested?.Invoke();
        }

        protected async Task<T> DesignOrRunTimeAsync<T>(Func<Task<T>> designTime, Func<Task<T>> runTime)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // in design mode
                return await designTime();
            }
            else
            {
                return await runTime();
            }
        }

        protected void DesignTime(Action action)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // in design mode
                action();
            }
        }

        protected async Task DesignTimeAsync(Func<Task> task)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // in design mode
                await task();
            }
        }

        protected void RunTime(Action action)
        {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // not in design mode
                action();
            }
        }

        protected async Task RunTimeAsync(Func<Task> task)
        {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // not in design mode
                await task();
            }
        }

        protected void Ui(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        protected async Task UiAsync(Action action)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }

        #endregion Methods
    }
}