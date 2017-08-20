using System.ComponentModel;
using System.Linq;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class BrushManager : Model.Model, IBrushManager
    {
        private bool selecting = false;

        public BrushManager(ArtView artView, ISelectionManager selectionManager)
        {
            ArtView = artView;

            selectionManager.Updated += (sender, args) =>
            {
                selecting = true;
                if (ArtView.SelectionManager.Selection.LastOrDefault() is Shape shape)
                {
                    Fill = shape.FillBrush;
                    Stroke = shape.StrokeBrush;
                    StrokeStyle = shape.StrokeStyle;
                    StrokeWidth = shape.StrokeWidth;
                }
                selecting = false;
            };

            PropertyChanged += OnPropertyChanged;
        }

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
            }
        }

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

        public SharpDX.Direct2D1.StrokeStyleProperties1 StrokeStyle
        {
            get => Get<SharpDX.Direct2D1.StrokeStyleProperties1>();
            set => Set(value);
        }

        public float StrokeWidth
        {
            get => Get<float>();
            set => Set(value);
        }
    }
}