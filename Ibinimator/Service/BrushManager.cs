using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public class BrushManager : Model.Model, IBrushManager
    {
        private bool selecting;

        public BrushManager(ArtView artView, ISelectionManager selectionManager)
        {
            ArtView = artView;
            StrokeDashes = new ObservableCollection<float>(new float [] { 0, 0, 0, 0 });

            selectionManager.Updated += (sender, args) =>
            {
                selecting = true;
                if (ArtView.SelectionManager.Selection.LastOrDefault() is Shape shape)
                {
                    Fill = shape.FillBrush;
                    Stroke = shape.StrokeBrush;
                    StrokeStyle = shape.StrokeStyle;
                    StrokeWidth = shape.StrokeWidth;
                    StrokeDashes = new ObservableCollection<float>(shape.StrokeDashes);
                }
                selecting = false;
            };

            PropertyChanged += OnPropertyChanged;
        }

        #region IBrushManager Members

        public ArtView ArtView { get; }

        public BrushInfo Fill
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public BrushInfo Stroke
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public StrokeStyleProperties1 StrokeStyle
        {
            get => Get<StrokeStyleProperties1>();
            set => Set(value);
        }

        public float StrokeWidth
        {
            get => Get<float>();
            set => Set(value);
        }

        public ObservableCollection<float> StrokeDashes
        {
            get => Get<ObservableCollection<float>>();
            private set => Set(value);
        }

        #endregion

        private void OnPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            // otherwise, selecting new shapes applies their properties to all
            // of the other selected shapes
            if (selecting) return;

            switch (args.PropertyName)
            {
                case nameof(Fill):
                    foreach (var layer in ArtView.SelectionManager.Selection.SelectMany(l => l.Flatten()))
                        if (layer is Shape shape)
                            shape.FillBrush = Fill;
                    break;
                case nameof(Stroke):
                    foreach (var layer in ArtView.SelectionManager.Selection.SelectMany(l => l.Flatten()))
                        if (layer is Shape shape)
                            shape.StrokeBrush = Stroke;
                    break;
                case nameof(StrokeStyle):
                    foreach (var layer in ArtView.SelectionManager.Selection.SelectMany(l => l.Flatten()))
                        if (layer is Shape shape)
                            shape.StrokeStyle = StrokeStyle;
                    break;
                case nameof(StrokeWidth):
                    foreach (var layer in ArtView.SelectionManager.Selection.SelectMany(l => l.Flatten()))
                        if (layer is Shape shape)
                            shape.StrokeWidth = StrokeWidth;
                    break;
                case nameof(StrokeDashes):
                    foreach (var layer in ArtView.SelectionManager.Selection.SelectMany(l => l.Flatten()))
                        if (layer is Shape shape)
                            shape.StrokeDashes = new ObservableCollection<float>(StrokeDashes);
                    break;
            }
        }
    }
}