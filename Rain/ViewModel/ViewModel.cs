using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Rain.ViewModel
{
    public abstract class ViewModel : Core.Model.Model
    {
        public event Action CloseRequested;

        public event Action HideRequested;

        public event Action ShowRequested;

        public void CloseView() { CloseRequested?.Invoke(); }

        public void HideView() { HideRequested?.Invoke(); }

        public void ShowView() { ShowRequested?.Invoke(); }

        protected async Task<T> DesignOrRunTimeAsync<T>(Func<Task<T>> designTime, Func<Task<T>> runTime)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return await designTime();

            return await runTime();
        }

        protected void DesignTime(Action action)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                action();
        }

        protected async Task DesignTimeAsync(Func<Task> task)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                await task();
        }

        protected void RunTime(Action action)
        {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                action();
        }

        protected async Task RunTimeAsync(Func<Task> task)
        {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                await task();
        }

        protected void Ui(Action action) { Application.Current.Dispatcher.Invoke(action); }

        protected async Task UiAsync(Action action) { await Application.Current.Dispatcher.InvokeAsync(action); }
    }
}