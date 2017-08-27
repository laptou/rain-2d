using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Service;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Mathematics.Interop;
using Rectangle = Ibinimator.Model.Rectangle;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel : ViewModel
    {
        public MainViewModel(ArtView artView)
        {
            FillPicker = new FillPickerViewModel(this);
            TransformPicker = new TransformViewModel(this);

            ViewManager = new ViewManager(artView);
            HistoryManager = new HistoryManager(artView);
            SelectionManager = new SelectionManager(artView, ViewManager, HistoryManager);
            BrushManager = new BrushManager(artView, SelectionManager);
            ToolManager = new ToolManager(artView);
            Load();

            artView.SetManager(ViewManager);
            artView.SetManager(BrushManager);
            artView.SetManager(SelectionManager);
            artView.SetManager(ToolManager);
            artView.SetManager(new CacheManager(artView));
            artView.SetManager(HistoryManager);

            SelectLayerCommand = new DelegateCommand<Layer>(SelectLayer, null);
            JumpHistoryCommand = new DelegateCommand<long>(id => HistoryManager.Time = id, null);
        }

        public MainViewModel()
        {
            RunTime(() => throw new InvalidOperationException(
                "This constructor only exists so that the XAML designer doesn't " +
                "complain. Do not call this."));
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

        public IHistoryManager HistoryManager
        {
            get => Get<IHistoryManager>();
            set
            {
                Set(value);
                BindingOperations.EnableCollectionSynchronization(value, value);
            }
        }

        public DelegateCommand<long> JumpHistoryCommand { get; }

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

        public DelegateCommand<Layer> SelectLayerCommand { get; }

        public IToolManager ToolManager
        {
            get => Get<IToolManager>();
            set => Set(value);
        }

        public TransformViewModel TransformPicker { get; }

        public IViewManager ViewManager
        {
            get => Get<IViewManager>();
            set
            {
                if (ViewManager != null)
                    ViewManager.DocumentUpdated -= LayerUpdated;

                Set(value);

                if (ViewManager != null)
                    ViewManager.DocumentUpdated += LayerUpdated;
            }
        }

        public event PropertyChangedEventHandler BrushUpdated;
        public event PropertyChangedEventHandler LayerUpdated;

        public event EventHandler SelectionUpdated;

        public void Load()
        {
            ViewManager.Root = new Group();
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

            var r2 = new Rectangle
            {
                X = 200,
                Y = 200,
                Width = 100,
                Height = 100,
                FillBrush = new SolidColorBrushInfo {Color = new RawColor4(0, 0.5f, 1f, 1f)},
                Rotation = MathUtil.Pi / 4
            };

            var p = new Path
            {
                X = 300,
                Y = 300,
                FillBrush = new SolidColorBrushInfo {Color = new RawColor4(0, 0.5f, 1f, 1f)},
                StrokeBrush = new SolidColorBrushInfo {Color = new RawColor4(0, 1f, 1f, 1f)},
                StrokeWidth = 5,
                Nodes = new ObservableCollection<PathNode>
                {
                    new PathNode {X = 100, Y = 100},
                    new PathNode {X = 150, Y = 100},
                    new PathNode {X = 150, Y = 150},
                    new PathNode {X = 200, Y = 150},
                    new PathNode {X = 200, Y = 200}
                }
            };

            l.Position = new Vector2(100, 100);

            ViewManager.Root.Add(l);

            l.Add(e);
            l.Add(r);
            l.Add(r2);
            l.Add(p);
        }

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
            else if (param != null)
                throw new ArgumentException(nameof(param));
        }
    }
}