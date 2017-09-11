using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Ibinimator.View;

namespace Ibinimator
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            SetDefaultFont();

            Icon.Initialize();
        }

        public static Dispatcher Dispatcher => Current.Dispatcher;

        public static bool IsDesigner =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        private void SetDefaultFont()
        {
            var font = new FontFamily(new Uri("pack://application:,,,/", UriKind.Absolute),
                "./Resources/Font/#Roboto");

            TextElement.FontFamilyProperty.OverrideMetadata(
                typeof(TextElement),
                new FrameworkPropertyMetadata(font));

            TextBlock.FontFamilyProperty.OverrideMetadata(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(font));
        }
    }
}