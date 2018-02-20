using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Rain.ViewModel;

namespace Rain.View
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
                                                   (s, e) => SystemCommands.CloseWindow(this),
                                                   (s, e) => e.CanExecute = true));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                                                   (s, e) => SystemCommands.MinimizeWindow(this),
                                                   (s, e) => e.CanExecute = true));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                                                   (s, e) => WindowState =
                                                                 WindowState ==
                                                                 WindowState.Maximized
                                                                     ? WindowState.Normal
                                                                     : WindowState.Maximized,
                                                   (s, e) => e.CanExecute = true));

            DataContext = new MainViewModel(ArtView);
        }
    }
}