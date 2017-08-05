using Ibinimator.View.Control;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Service;
using Ibinimator.ViewModel;

namespace Ibinimator.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));

#pragma warning disable IDE0017 // Simplify object initialization
            var viewModel = new MainViewModel();
#pragma warning restore IDE0017 // Simplify object initialization
            viewModel.ViewManager = new ViewManager(this.ArtView);
            viewModel.SelectionManager = new SelectionManager(this.ArtView, viewModel.ViewManager);
            viewModel.BrushManager = new BrushManager(this.ArtView, viewModel.SelectionManager);
            viewModel.ToolManager = new ToolManager(this.ArtView);
            viewModel.Load();

            this.ArtView.SetManager(viewModel.ViewManager);
            this.ArtView.SetManager(viewModel.BrushManager);
            this.ArtView.SetManager(viewModel.SelectionManager);
            this.ArtView.SetManager(viewModel.ToolManager);
            this.ArtView.SetManager(new CacheManager(this.ArtView));

            DataContext = viewModel;
        }
    }
}