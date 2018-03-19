using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

using Rain.Service;

namespace Rain
{
    public class DebugTraceListener : TraceListener
    {
        public override void Write(string message) { }

        public override void WriteLine(string message)
        {
            // Debugger.Break();
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        public App()
        {
            _startTime = Stopwatch.GetTimestamp();

            LogEvent("App..ctor() called.");

            InitializeComponent();

            LogEvent("App.IntializeComponent() completed.");

            AppSettings.BeginLoadDefault();

            LogEvent("AppSettings.BeginLoadDefault() completed.");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private readonly long _startTime;

        public static Dispatcher CurrentDispatcher => Current.Dispatcher;

        public new static App Current => Application.Current as App;

        public static bool IsDesigner =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        protected override void OnStartup(StartupEventArgs e)
        {
            LogEvent("App.OnStartup() called.");

            base.OnStartup(e);

            LogEvent("Application.OnStartup() completed.");

            SetDefaultFont();

            LogEvent("App.OnStartup() completed.");
        }

        public void LogEvent(string evt)
        {
            var time = Stopwatch.GetTimestamp() - _startTime;
            var seconds = time / (double) Stopwatch.Frequency;

            #if DEBUG
            Trace.WriteLine($"[{seconds * 1000:F2}ms]: {evt}");
            #else
            Console.WriteLine($"[{seconds * 1000:F2}ms]: {evt}");
            #endif
        }

        private static async Task LogError(object o, int level)
        {
            // log that bitch
            using (var writer = new StreamWriter(File.Open("rain.log", FileMode.Append)))
            {
                await LogError(o, level, writer);
            }
        }

        private static async Task LogError(object o, int level, StreamWriter writer)
        {
            var ex = o as Exception;

            await writer.WriteLineAsync(new string('\t', level - 1) + $"Error [{DateTime.Now}]");

            if (ex != null)
            {
                await writer.WriteLineAsync(new string('\t', level) +
                                            $"Type: {ex.GetType().AssemblyQualifiedName}");
                await writer.WriteLineAsync(new string('\t', level) + $"Message: {ex.Message}");
                await writer.WriteLineAsync(new string('\t', level) + $"HResult: 0x{ex.HResult:X}");
                await writer.WriteLineAsync(new string('\t', level) + $"Source: {ex.Source}");

                var stack = ex.StackTrace.Replace(Environment.NewLine,
                                                  Environment.NewLine + new string('\t', level));

                await writer.WriteLineAsync(new string('\t', level) + $"Stack Trace:\r\n{stack}");

                if (ex.InnerException != null)
                {
                    await writer.WriteLineAsync(new string('\t', level) + "Inner Exception: ");
                    await LogError(ex.InnerException, level + 1, writer);
                }
            }
            else
            {
                await writer.WriteLineAsync(new string('\t', level) + $"{o}");
            }

            await writer.FlushAsync();
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Task.Run(async () => await LogError(e.ExceptionObject, 1)).Wait();

            // wait on the task because if this handler returns, the process exits
        }

        private void SetDefaultFont()
        {
            var font = new FontFamily(new Uri("pack://application:,,,/", UriKind.Absolute),
                                      "./Resources/Font/#Roboto");

            TextElement.FontFamilyProperty.OverrideMetadata(typeof(TextElement),
                                                            new FrameworkPropertyMetadata(font));

            TextBlock.FontFamilyProperty.OverrideMetadata(typeof(TextBlock),
                                                          new FrameworkPropertyMetadata(font));
        }
    }
}