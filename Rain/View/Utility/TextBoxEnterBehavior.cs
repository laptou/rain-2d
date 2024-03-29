﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Rain.View.Utility
{
    public class TextBoxEnterBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            if (AssociatedObject == null) return;

            base.OnAttached();
            AssociatedObject.GotKeyboardFocus += OnAssociatedObjectGotFocus;
            AssociatedObject.GotMouseCapture += OnAssociatedObjectGotFocus;
            AssociatedObject.LostKeyboardFocus += OnAssiciatedObjectLostFocus;
            AssociatedObject.KeyDown += OnAssociatedObjectKeyDown;
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject == null) return;

            AssociatedObject.GotKeyboardFocus -= OnAssociatedObjectGotFocus;
            AssociatedObject.GotMouseCapture -= OnAssociatedObjectGotFocus;
            AssociatedObject.LostMouseCapture -= OnAssiciatedObjectLostFocus;
            AssociatedObject.LostKeyboardFocus -= OnAssiciatedObjectLostFocus;
            AssociatedObject.KeyDown -= OnAssociatedObjectKeyDown;
            base.OnDetaching();
        }

        private static void OnAssiciatedObjectLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.Select(0, 0);
        }

        private static void OnAssociatedObjectGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.SelectAll();
        }

        private static void OnAssociatedObjectKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is FrameworkElement element)
                switch (e.Key)
                {
                    case Key.Return:
                        if (!element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)))
                            Keyboard.ClearFocus();

                        break;
                    case Key.Escape:
                        Keyboard.ClearFocus();

                        break;
                }
        }
    }
}