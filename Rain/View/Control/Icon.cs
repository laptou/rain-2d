using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Rain.Service;

namespace Rain.View.Control
{
    public class Icon : SvgImage
    {
        public static readonly DependencyProperty InvertedProperty =
            DependencyProperty.Register("Inverted",
                                        typeof(bool),
                                        typeof(Icon),
                                        new FrameworkPropertyMetadata(false,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender,
                                                                      IconChanged));

        public static readonly DependencyProperty IconNameProperty =
            DependencyProperty.Register("IconName",
                                        typeof(string),
                                        typeof(Icon),
                                        new FrameworkPropertyMetadata(null,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender,
                                                                      IconChanged));

        static Icon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Icon), new FrameworkPropertyMetadata(typeof(Icon)));
        }

        public string IconName
        {
            get => (string) GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        public bool Inverted
        {
            get => (bool) GetValue(InvertedProperty);
            set => SetValue(InvertedProperty, value);
        }

        private static void IconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Icon icon &&
                icon.IconName != null)
            {
                var theme = AppSettings.Current.GetString("theme");

                if (icon.Inverted)
                    switch (theme)
                    {
                        case "dark":
                            theme = "light";

                            break;
                        case "light":
                            theme = "dark";

                            break;
                    }

                if (string.IsNullOrWhiteSpace(theme))
                    throw new Exception();

                icon.Source = new Uri($"/Rain;component/Resources/Icon/{icon.IconName}-{theme}.svg", UriKind.Relative);
            }
        }
    }
}