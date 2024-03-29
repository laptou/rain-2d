﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using FPMO = System.Windows.FrameworkPropertyMetadataOptions;

namespace Rain.View.Utility
{
    public class Helper
    {
        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.RegisterAttached("Accent",
                                                typeof(Color),
                                                typeof(Helper),
                                                new FrameworkPropertyMetadata(
                                                    Color.FromRgb(255, 255, 255),
                                                    FPMO.Inherits | FPMO.AffectsRender));

        public static readonly DependencyProperty InputBindingSourceProperty =
            DependencyProperty.RegisterAttached("InputBindingSource",
                                                typeof(IEnumerable<InputBinding>),
                                                typeof(Helper),
                                                new PropertyMetadata(InputBindingsChanged));

        public static readonly DependencyProperty ElevationProperty =
            DependencyProperty.RegisterAttached("Elevation",
                                                typeof(int),
                                                typeof(Helper),
                                                new FrameworkPropertyMetadata(0, FPMO.AffectsRender));

        public static readonly DependencyProperty StoryboardTraceProperty =
            DependencyProperty.RegisterAttached("StoryboardTrace",
                                                typeof(bool),
                                                typeof(Helper),
                                                new PropertyMetadata(false, StorybardTraceChanged));

        public static Color GetAccent(DependencyObject obj) { return (Color) obj.GetValue(AccentProperty); }

        public static int GetElevation(DependencyObject element) { return (int) element.GetValue(ElevationProperty); }

        public static IEnumerable<InputBinding> GetInputBindingSource(DependencyObject element)
        {
            return (IEnumerable<InputBinding>) element.GetValue(InputBindingSourceProperty);
        }

        public static bool GetStoryboardTrace(UIElement element)
        {
            return (bool) element.GetValue(StoryboardTraceProperty);
        }

        public static void SetAccent(DependencyObject obj, Color value) { obj.SetValue(AccentProperty, value); }

        public static void SetElevation(DependencyObject element, int value)
        {
            element.SetValue(ElevationProperty, value);
        }

        public static void SetInputBindingSource(DependencyObject element, IEnumerable<InputBinding> value)
        {
            element.SetValue(InputBindingSourceProperty, value);
        }

        public static void SetStoryboardTrace(UIElement element, bool value)
        {
            element.SetValue(StoryboardTraceProperty, value);
        }

        private static void InputBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (IEnumerable<InputBinding>) e.NewValue;

            if (d is UIElement ui)
            {
                ui.InputBindings.Clear();

                foreach (var inputBinding in value)
                    ui.InputBindings.Add(inputBinding);
            }
        }

        private static void StorybardTraceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Storyboard storyboard)
            {
                if (e.NewValue as bool? == true)
                {
                    storyboard.Completed += StoryboardOnCompleted;
                    storyboard.CurrentStateInvalidated += StoryboardOnCurrentStateInvalidated;
                }
                else
                {
                    storyboard.Completed -= StoryboardOnCompleted;
                    storyboard.CurrentStateInvalidated -= StoryboardOnCurrentStateInvalidated;
                }
            }
        }

        private static void StoryboardOnCompleted(object sender, EventArgs eventArgs)
        {
            if (sender is Storyboard sb) Trace.WriteLine($"SB:: {DateTime.Now}: Storyboard {sb.Name} has completed.");
        }

        private static void StoryboardOnCurrentStateInvalidated(object sender, EventArgs eventArgs)
        {
            if (sender is Storyboard sb)
                Trace.WriteLine(
                    $"SB:: {DateTime.Now}: Storyboard {sb.Name} is now in clock state {sb.GetCurrentState()}.");
        }
    }

    public class PercentageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fraction = System.Convert.ToDecimal(value);

            return fraction.ToString("P0", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal.TryParse(value.ToString().Trim(culture.NumberFormat.PercentSymbol[0]),
                             NumberStyles.Any,
                             culture.NumberFormat,
                             out var d);

            return d / 100;
        }

        #endregion
    }
}