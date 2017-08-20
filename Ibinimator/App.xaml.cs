using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ibinimator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsDesigner =>
            System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;

        public static Dispatcher Dispatcher => Current.Dispatcher;


        public App()
        {
            InitializeComponent();

            SetDefaultFont();
        }

        private void SetDefaultFont()
        {
            var font = new FontFamily(new Uri("pack://application:,,,/", UriKind.Absolute), "./Resources/Fonts/#Roboto");

            TextElement.FontFamilyProperty.OverrideMetadata(
                typeof(TextElement),
                new FrameworkPropertyMetadata(font));

            TextBlock.FontFamilyProperty.OverrideMetadata(
                typeof(TextBlock),
                new FrameworkPropertyMetadata(font));
        }
    }
}