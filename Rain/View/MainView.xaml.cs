using System;
using System.Collections.Generic;
using System.Linq;

using Rain.Native;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Rain.Service;
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
            App.LogEvent("MainView..ctor() called.");

            // blocks on settings loading b/c otherwise utility:Theme
            // markup extensions won't work
            AppSettings.EndLoadDefault();

            App.LogEvent("AppSettings.EndLoadDefault() completed.");

            InitializeComponent();

            App.LogEvent("MainView.InitializeComponent() completed.");

            ViewModel = new MainViewModel(ArtView);

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
                                                   (s, e) => SystemCommands.CloseWindow(this),
                                                   (s, e) => e.CanExecute = true));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                                                   (s, e) => SystemCommands.MinimizeWindow(this),
                                                   (s, e) => e.CanExecute = true));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                                                   (s, e) => WindowState =
                                                                 WindowState == WindowState.Maximized
                                                                     ? WindowState.Normal
                                                                     : WindowState.Maximized,
                                                   (s, e) => e.CanExecute = true));
        }

        public MainViewModel ViewModel
        {
            get => DataContext as MainViewModel;
            set => DataContext = value;
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (!ViewModel.Initialized)
            {
                ViewModel.Initialize();
                App.LogEvent("MainView.DataContext initialized.");
            }
        }

        /// <inheritdoc />
        protected override void OnContentRendered(EventArgs e)
        {
            App.LogEvent("MainView.OnContentRendered() called.");

            base.OnContentRendered(e);
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            InitializeBlurBehind();

            base.OnSourceInitialized(e);
        }

        private void InitializeBlurBehind()
        {
            // HWND has been created, but window is not visible yet

            var interop = new WindowInteropHelper(this);

            var accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.EnableBlurBehind,
                AccentFlags = 2,
                GradientColor = 0x20333333u
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

                if (hr != 0)
                    NativeHelper.CheckError();
            }

            App.LogEvent("Blur-behind initialized.");
        }
    }
}