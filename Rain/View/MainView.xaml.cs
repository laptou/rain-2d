using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Rain.Native;
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

            var interop = new WindowInteropHelper(this);

            var accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.EnableBlurBehind,
                AccentFlags = 2,
                GradientColor = 0x2058D432u
            };

            // Fluent design is not available before RS4 (Spring Creators Update)
            if (VersionHelper.RequireVersion(10, 0, 17063, 0))
                accentPolicy.AccentState = AccentState.EnableFluent;

            using (var ptr = accentPolicy.ToSmartPtr())
            {
                var cdata = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.AccentPolicy,
                    Data = ptr,
                    SizeOfData = ptr.Size
                };
                
                var hr = WindowHelper.SetWindowCompositionAttribute(interop.EnsureHandle(), ref cdata);

                if(hr != 0)
                    NativeHelper.CheckError();
            }
        }
    }
}