using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Mathematics.Interop;
using Rectangle = Ibinimator.Model.Rectangle;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel : ViewModel
    {
        #region Constructors

        public MainViewModel()
        {
            FillPicker = new FillPickerViewModel(this);
            TransformPicker = new TransformViewModel(this);

            SelectLayerCommand = new DelegateCommand(SelectLayer);
        }

        #endregion Constructors

        public event EventHandler SelectionUpdated;
        public event PropertyChangedEventHandler LayerUpdated;
        public event PropertyChangedEventHandler BrushUpdated;

        #region Properties

        public IViewManager ViewManager
        {
            get => Get<IViewManager>();
            set
            {
                if (ViewManager != null)
                    ViewManager.LayerUpdated -= LayerUpdated;

                Set(value);

                if (ViewManager != null)
                    ViewManager.LayerUpdated += LayerUpdated;
            }
        }

        public ISelectionManager SelectionManager
        {
            get => Get<ISelectionManager>();
            set
            {
                if (SelectionManager != null)
                    SelectionManager.Updated -= SelectionUpdated;

                Set(value);

                if (SelectionManager != null)
                    SelectionManager.Updated += SelectionUpdated;
            }
        }

        public IToolManager ToolManager
        {
            get => Get<IToolManager>();
            set => Set(value);
        }

        public IBrushManager BrushManager
        {
            get => Get<IBrushManager>();
            set
            {
                if (BrushManager != null)
                    BrushManager.PropertyChanged -= BrushUpdated;

                Set(value);

                if (BrushManager != null)
                    BrushManager.PropertyChanged += BrushUpdated;
            }
        }

        public FillPickerViewModel FillPicker { get; }

        public TransformViewModel TransformPicker { get; }

        public DelegateCommand SelectLayerCommand { get; }

        #endregion Properties

        #region Methods

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Layer layer)
            {
                if (e.PropertyName == nameof(Layer.Selected))
                {
                    var contains = SelectionManager.Selection.Contains(layer);

                    if (layer.Selected && !contains)
                        SelectionManager.Selection.Add(layer);
                    else if (!layer.Selected && contains)
                        SelectionManager.Selection.Remove(layer);
                }
            }
            else
            {
                throw new ArgumentException("What?!");
            }
        }

        private void SelectLayer(object param)
        {
            if (param is Layer layer)
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    layer.Selected = !layer.Selected;
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (SelectionManager.Selection.Count > 0)
                    {
                        var inRange = false;

                        foreach (var l in SelectionManager.Selection[0].Parent.SubLayers)
                        {
                            if (inRange)
                                l.Selected = true;

                            if (l == layer || l == SelectionManager.Selection[0])
                                inRange = !inRange;
                        }
                    }
                }
                else
                {
                    SelectionManager.ClearSelection();

                    layer.Selected = true;
                }
            else throw new ArgumentException(nameof(param));
        }

        public void Load()
        {
            ViewManager.Root = new Layer();
            ViewManager.Root.PropertyChanged += OnLayerPropertyChanged;

            var l = new Group();

            var e = new Ellipse
            {
                X = 100,
                Y = 100,
                RadiusX = 50,
                RadiusY = 50,
                FillBrush = new SolidColorBrushInfo {Color = new RawColor4(1f, 1f, 0, 1f)},
                StrokeBrush = new SolidColorBrushInfo {Color = new RawColor4(1f, 0, 0, 1f)},
                StrokeWidth = 5,
                Rotation = MathUtil.Pi
            };
            e.UpdateTransform();

            var r = new Rectangle
            {
                X = 150,
                Y = 150,
                Width = 100,
                Height = 100,
                FillBrush = new SolidColorBrushInfo {Color = new RawColor4(1f, 0, 1f, 1f)},
                StrokeBrush = new SolidColorBrushInfo {Color = new RawColor4(0, 1f, 1f, 1f)},
                StrokeWidth = 5
            };
            r.UpdateTransform();

            var r2 = new Rectangle
            {
                X = 200,
                Y = 200,
                Width = 100,
                Height = 100,
                FillBrush = new SolidColorBrushInfo {Color = new RawColor4(0, 0.5f, 1f, 1f)},
                Rotation = MathUtil.Pi / 4
            };
            r2.UpdateTransform();

            var p = new Path
            {
                X = 300,
                Y = 300,
                FillBrush = new SolidColorBrushInfo { Color = new RawColor4(0, 0.5f, 1f, 1f) },
                StrokeBrush = new SolidColorBrushInfo { Color = new RawColor4(0, 1f, 1f, 1f) },
                StrokeWidth = 5,
                Nodes = new ObservableCollection<PathNode>
                {
                    new PathNode { X = 100, Y = 100 },
                    new PathNode { X = 150, Y = 100 },
                    new PathNode { X = 150, Y = 150 },
                    new PathNode { X = 200, Y = 150 },
                    new PathNode { X = 200, Y = 200 }
                }
            };
            r2.UpdateTransform();

            l.Position = new Vector2(100, 100);
            l.UpdateTransform();

            ViewManager.Root.Add(l);

            l.Add(e);
            l.Add(r);
            l.Add(r2);
            l.Add(p);
        }

        #endregion Methods
    }
}