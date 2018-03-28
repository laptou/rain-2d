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
        private static StreamWriter log;

        [STAThread]
        public static void Main()
        {
            _startTime = Stopwatch.GetTimestamp();

            log = new StreamWriter(File.Open("rain.log",
                                             FileMode.Create,
                                             FileAccess.ReadWrite,
                                             FileShare.Read));

            LogEvent("App.Main() called.");

            var app = new App();
            app.InitializeComponent();
            app.Run();

            LogEvent("App shutting down.");

            log.Flush();
        }

        public App()
        {
            LogEvent("App..ctor() called.");

            InitializeComponent();

            LogEvent("App.IntializeComponent() completed.");

            AppSettings.BeginLoadDefault();

            LogEvent("AppSettings.BeginLoadDefault() completed.");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static long _startTime;

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

        public static void LogEvent(string evt)
        {
            var time = Stopwatch.GetTimestamp() - _startTime;
            var seconds = time / (double) Stopwatch.Frequency;

            #if DEBUG
            Trace.WriteLine($"Event [{DateTime.Now}, {seconds * 1000:F2}ms]: {evt}", "Event");
            #else
            log.WriteLine($"Event [{DateTime.Now}, {seconds * 1000:F2}ms]: {evt}");
            #endif
        }

        private static async Task LogError(object o, int level)
        {
            var indent = new string('\t', level - 1);
            var time = Stopwatch.GetTimestamp() - _startTime;
            var seconds = time / (double)Stopwatch.Frequency;

            await log.WriteLineAsync(indent + $"Error [{DateTime.Now}, {seconds * 1000:F2}ms]");

            indent += "\t";

            if (o is Exception ex)
            {
                await log.WriteLineAsync(indent + $"Type: {ex.GetType().AssemblyQualifiedName}");
                await log.WriteLineAsync(indent + $"Message: {ex.Message}");
                await log.WriteLineAsync(indent + $"HResult: 0x{ex.HResult:X}");
                await log.WriteLineAsync(indent + $"Source: {ex.Source}");

                var stack =
                    ex.StackTrace.Replace(Environment.NewLine, Environment.NewLine + indent);

                await log.WriteLineAsync(indent + $"Stack Trace:\r\n{stack}");

                if (ex.InnerException != null)
                {
                    await log.WriteLineAsync(indent + "Inner Exception: ");
                    await LogError(ex.InnerException, level + 1);
                }
            }
            else
            {
                await log.WriteLineAsync(indent + $"{o}");
            }

            await log.FlushAsync();
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError(e.ExceptionObject, 1).Wait();

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