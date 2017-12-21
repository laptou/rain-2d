using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Ibinimator.Native;
using Ibinimator.ViewModel;

namespace Ibinimator.View
{
    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
                                                   (s, e) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                                                   (s, e) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                                                   (s, e) => WindowState =
                                                                 WindowState == WindowState.Maximized
                                                                     ? WindowState.Normal
                                                                     : WindowState.Maximized));

            DataContext = new MainViewModel(ArtView);
        }
    }
}