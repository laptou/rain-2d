using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Ibinimator.Service;

namespace Ibinimator.View.Control
{
    public class Icon : SvgImage
    {
        public static readonly DependencyProperty InvertedProperty =
            DependencyProperty.Register("Inverted", typeof(bool), typeof(Icon),
                                        new FrameworkPropertyMetadata(
                                            false, FrameworkPropertyMetadataOptions.AffectsRender, IconChanged));

        public static readonly DependencyProperty IconNameProperty =
            DependencyProperty.Register("IconName", typeof(string), typeof(Icon),
                                        new FrameworkPropertyMetadata(
                                            null, FrameworkPropertyMetadataOptions.AffectsRender, IconChanged));

        static Icon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Icon),
                                                     new FrameworkPropertyMetadata(typeof(Icon)));
        }

        private static void IconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Icon icon)
            {
                var theme = Settings.GetString("theme");

                if (icon.Inverted)
                {
                    switch (theme)
                    {
                        case "dark":
                            theme = "light";

                            break;
                        case "light":
                            theme = "dark";

                            break;
                    }
                }

                icon.Source =
                    new Uri($"/Ibinimator;component/Resources/Icon/{icon.IconName}-{theme}.svg", UriKind.Relative);
            }
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
    }
}