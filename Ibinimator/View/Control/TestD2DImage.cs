using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Collections;

using System.Windows;

namespace Ibinimator.View.Control
{
    public class TestD2DImage : D2DImage
    {
        private Ellipse ellipse;

        public TestD2DImage() : base()
        {
            ellipse = new Ellipse(new RawVector2(50f, 50f), 50f, 50f);

            RenderTargetBound += OnRenderTargetBound;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnRenderTargetBound(object sender, RenderTarget target)
        {
            EnableAntialiasing = true;
            LoadBrushes(target);
        }

        private void LoadBrushes(RenderTarget target)
        {
            foreach (DictionaryEntry entry in Application.Current.Resources)
            {
                if (entry.Value is System.Windows.Media.Color color)
                {
                    Brushes[entry.Key as string] =
                        new SolidColorBrush(
                            target,
                            new RawColor4(
                                color.R / 255f,
                                color.G / 255f,
                                color.B / 255f,
                                color.A / 255f));
                }
            }
        }

        protected override void Render(RenderTarget target)
        {
            target.DrawEllipse(ellipse, Brushes["A1"]);
        }

        public Dictionary<string, Brush> Brushes { get; set; } = new Dictionary<string, Brush>();
    }
}