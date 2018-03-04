using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

using Rain.Core.Utility;

using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Renderer;
using Rain.Service;
using Rain.Tools;
using Rain.View.Command;
using Rain.View.Control;

namespace Rain.ViewModel
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(ArtView artView)
        {
            var cache = new CacheManager(artView.ArtContext);
            ViewManager = new ViewManager(artView.ArtContext);
            HistoryManager = new HistoryManager(artView.ArtContext);
            SelectionManager = new SelectionManager(artView.ArtContext);
            ToolManager = new ToolManager(artView.ArtContext);
            BrushManager = new BrushManager(artView.ArtContext);

            ArtContext = artView.ArtContext;

            ViewManager.Root = Load();

            artView.ArtContext.SetManager(cache);
            artView.ArtContext.SetManager(ViewManager);
            artView.ArtContext.SetManager(HistoryManager);
            artView.ArtContext.SetManager(SelectionManager);
            artView.ArtContext.SetManager(ToolManager);
            artView.ArtContext.SetManager(BrushManager);


            ColorViewModel = new ColorViewModel(ArtContext);
            TransformViewModel = new TransformViewModel(ArtContext);
            StrokeViewModel = new StrokeViewModel(ArtContext);

            ToolManager.Type = ToolType.Select;

            SelectLayerCommand = new DelegateCommand<Layer>(SelectLayer, null);
            JumpHistoryCommand =
                new DelegateCommand<long>(id => HistoryManager.Position = id, null);

            MenuItems = LoadMenus("menus").ToList();
            ToolbarItems = LoadToolbars("toolbars").ToList();
        }

        public MainViewModel()
        {
            RunTime(() => throw new InvalidOperationException(
                              "This constructor only exists so that the XAML designer doesn't " +
                              "complain. Do not call this."));

            AppSettings.LoadDefault();

            MenuItems = LoadMenus("menus").ToList();
            ToolbarItems = LoadToolbars("toolbars").ToList();
        }

        public IArtContext ArtContext { get; set; }

        public event PropertyChangedEventHandler BrushUpdated;

        public event PropertyChangedEventHandler LayerUpdated;

        public event EventHandler SelectionUpdated;

        public IContainerLayer Load()
        {
            var root = new Group();

            var l = new Group();

            var e = new Ellipse
            {
                CenterX = 150,
                CenterY = 150,
                RadiusX = 50,
                RadiusY = 50,
                Fill = new SolidColorBrushInfo {Color = new Color(1f, 1f, 0, 1f)},
                Stroke = new PenInfo
                {
                    Width = 5,
                    Brush = new SolidColorBrushInfo {Color = new Color(1f, 0, 0, 1f)}
                }
            };
            e.ApplyTransform(Matrix3x2.CreateRotation(MathUtils.Pi));

            var r = new Rectangle
            {
                X = 150,
                Y = 150,
                Width = 100,
                Height = 100,
                Fill = new SolidColorBrushInfo {Color = new Color(1f, 0, 1f, 1f)},
                Stroke = new PenInfo
                {
                    Width = 5,
                    Brush = new SolidColorBrushInfo {Color = new Color(0, 1f, 1f, 1f)}
                }
            };

            var r2 = new Rectangle
            {
                X = 200,
                Y = 200,
                Width = 100,
                Height = 100,
                Fill = new SolidColorBrushInfo {Color = new Color(0, 0.5f, 1f, 1f)}
            };
            r2.ApplyTransform(Matrix3x2.CreateRotation(MathUtils.Pi / 4));

            var p = new Path
            {
                Fill = new SolidColorBrushInfo {Color = new Color(0, 0.5f, 1f, 1f)},
                Stroke = new PenInfo
                {
                    Brush = new SolidColorBrushInfo {Color = new Color(0, 1f, 1f, 1f)},
                    Width = 5
                },
                Instructions =
                {
                    new MovePathInstruction(100, 100),
                    new LinePathInstruction(150, 100),
                    new LinePathInstruction(150, 150),
                    new LinePathInstruction(200, 150),
                    new LinePathInstruction(200, 200)
                }
            };
            p.ApplyTransform(Matrix3x2.CreateTranslation(300, 300));

            var t = new Text
            {
                Value = "hello world",
                Fill = r2.Fill,
                TextStyle = new TextInfo
                {
                    FontFamily = "Roboto",
                    FontSize = 32
                },
                Width = 100,
                Height = 100
            };
            t.ApplyTransform(Matrix3x2.CreateTranslation(200, 200));

            ViewManager.Document.Bounds = new RectangleF(0, 0, 600, 600);

            l.ApplyTransform(Matrix3x2.CreateTranslation(100, 100));

            root.Add(t);

            var reference1 = new Clone
            {
                Target = t,
                Override = true
            };

            var reference2 = new Clone
            {
                Target = t,
                Override = true
            };

            var reference3 = new Clone
            {
                Target = reference1,
                Override = true
            };

            reference1.ApplyTransform(Matrix3x2.CreateTranslation(100, 100));
            reference2.ApplyTransform(Matrix3x2.CreateTranslation(100, -100));
            reference3.ApplyTransform(Matrix3x2.CreateTranslation(100, -100));
            reference2.Fill = new SolidColorBrushInfo(new Color(1, 0, 0));

            var pic = new Picture();
            pic.ApplyTransform(Matrix3x2.CreateScale(0.25f) * Matrix3x2.CreateTranslation(300, 200));
            pic.Image = ArtContext.ResourceContext.LoadImageFromFilename(@"C:\Users\ibiyemi.CENTRAL\Pictures\anders-jilden-307322-unsplash.jpg");

            root.Add(reference1);
            root.Add(reference2);
            root.Add(reference3);
            root.Add(l);
            root.Add(pic);

            l.Add(e);
            l.Add(r);
            l.Add(r2);
            l.Add(p);

            return root;
        }

        private IEnumerable<MenuItem> LoadMenus(string path)
        {
            var settings = AppSettings.Current;

            if (!settings.Contains(path + ".$count"))
                yield break;

            var menuCount = settings.GetInt(path + ".$count");

            for (var i = 0; i < menuCount; i++)
            {
                var localPath = path + "[" + i + "]";

                if (settings.Contains(localPath) &&
                    settings[localPath] == null)
                {
                    yield return new MenuItem();
                }
                else
                {
                    var name = settings.GetString(localPath + ".name");
                    var cmd = settings.GetString(localPath + ".command");
                    var shortcuts = cmd == null
                                        ? Enumerable.Empty<string>()
                                        : Enumerable
                                         .Range(0,
                                                (int) settings.GetFloat(
                                                    "shortcuts." + cmd + ".$count"))
                                         .Select(j => settings.GetString(
                                                     "shortcuts." + cmd + $"[{j}]"))
                                         .ToList();

                    var item = new MenuItem(name,
                                            LoadMenus(localPath + ".menus"),
                                            MapCommand(cmd),
                                            shortcuts.FirstOrDefault(),
                                            ArtContext);

                    yield return item;

                    if (item.Command == null ||
                        item.Shortcut == null) continue;

                    foreach (var shortcut in shortcuts)
                    {
                        var gesture = ShortcutHelper.GetGesture(shortcut);
                        var binding = new InputBinding(item.Command, gesture)
                        {
                            CommandParameter = ArtContext
                        };

                        Shortcuts.Add(binding);
                    }
                }
            }
        }

        private IEnumerable<ToolbarItem> LoadToolbars(string path)
        {
            var settings = AppSettings.Current;

            if (!settings.Contains(path + ".$count"))
                yield break;

            var tbCount = settings.GetInt(path + ".$count");

            for (var i = 0; i < tbCount; i++)
            {
                var localPath = path + "[" + i + "]";
                var type = settings.GetEnum<ToolbarItemType>(localPath + ".type");

                switch (type)
                {
                    case ToolbarItemType.Space:

                        yield return new ToolbarItem(ArtContext);

                        break;
                    case ToolbarItemType.Button:

                        yield return new ToolbarItem(settings.GetString(localPath + ".name"),
                                                     MapCommand(
                                                         settings.GetString(
                                                             localPath + ".command")),
                                                     settings.GetString(localPath + ".icon"),
                                                     ArtContext);

                        break;
                    default:

                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static ICommand MapCommand(string name)
        {
            switch (name)
            {
                case "file.open":             return FileCommands.OpenCommand;
                case "file.save":             return FileCommands.SaveCommand;
                case "edit.undo":             return HistoryCommands.UndoCommand;
                case "edit.redo":             return HistoryCommands.RedoCommand;
                case "object.union":          return ObjectCommands.UnionCommand;
                case "object.difference":     return ObjectCommands.DifferenceCommand;
                case "object.intersection":   return ObjectCommands.IntersectionCommand;
                case "object.exclusion":      return ObjectCommands.XorCommand;
                case "object.pathify":        return ObjectCommands.PathifyCommand;
                case "selection.group":       return SelectionCommands.GroupCommand;
                case "selection.ungroup":     return SelectionCommands.UngroupCommand;
                case "selection.move-bottom": return SelectionCommands.MoveToBottomCommand;
                case "selection.move-down":   return SelectionCommands.MoveDownCommand;
                case "selection.move-up":     return SelectionCommands.MoveUpCommand;
                case "selection.move-top":    return SelectionCommands.MoveToTopCommand;
                case "selection.mirror-x":    return SelectionCommands.FlipHorizontalCommand;
                case "selection.mirror-y":    return SelectionCommands.FlipVerticalCommand;
                case "selection.rotate-cw":   return SelectionCommands.RotateClockwiseCommand;
                case "selection.rotate-ccw":

                    return SelectionCommands.RotateCounterClockwiseCommand;
                case "selection.align-left":     return SelectionCommands.AlignLeftCommand;
                case "selection.align-right":    return SelectionCommands.AlignRightCommand;
                case "selection.align-top":      return SelectionCommands.AlignTopCommand;
                case "selection.align-bottom":   return SelectionCommands.AlignBottomCommand;
                case "selection.align-center-x": return SelectionCommands.AlignCenterXCommand;
                case "selection.align-center-y": return SelectionCommands.AlignCenterYCommand;
                case "help.license":             return ViewCommands.LicenseCommand;
                default:                         return null;
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
                    if (SelectionManager.Selection.Any())
                    {
                        var inRange = false;
                        var first = SelectionManager.Selection.First();

                        foreach (var l in first.Parent.SubLayers)
                        {
                            if (inRange)
                                l.Selected = true;

                            if (l == layer ||
                                l == first)
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


        #region Input

        public List<MenuItem> MenuItems { get; set; }

        public List<InputBinding> Shortcuts { get; } = new List<InputBinding>();

        public List<ToolbarItem> ToolbarItems { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<long> JumpHistoryCommand { get; }

        public DelegateCommand<Layer> SelectLayerCommand { get; }

        #endregion

        #region ArtContext Managers

        public IHistoryManager HistoryManager
        {
            get => Get<IHistoryManager>();
            set
            {
                Set(value);
                BindingOperations.EnableCollectionSynchronization(value, value);
            }
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

        public ISelectionManager SelectionManager
        {
            get => Get<ISelectionManager>();
            set
            {
                if (SelectionManager != null)
                    SelectionManager.SelectionChanged -= SelectionUpdated;

                Set(value);

                if (SelectionManager != null)
                    SelectionManager.SelectionChanged += SelectionUpdated;
            }
        }

        public IToolManager ToolManager
        {
            get => Get<IToolManager>();
            set => Set(value);
        }

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

        #endregion

        #region Sub View Models

        public TransformViewModel TransformViewModel { get; }
        public StrokeViewModel StrokeViewModel { get; }
        public ColorViewModel ColorViewModel { get; }

        #endregion
    }

    public class MenuItem
    {
        public MenuItem() { Type = MenuItemType.Separator; }

        public MenuItem(
            string name, IEnumerable<MenuItem> subMenus, ICommand command, string shortcut,
            IArtContext artContext)
        {
            SubMenus = subMenus.ToList();
            Command = command;
            ArtContext = artContext;
            Name = name;
            Shortcut = ShortcutHelper.Prettify(shortcut);
            Type = MenuItemType.Item;
        }

        public IArtContext ArtContext { get; }

        public ICommand Command { get; }

        public string Name { get; }

        public string Shortcut { get; }

        public IList<MenuItem> SubMenus { get; set; }

        public MenuItemType Type { get; }
    }

    public class ToolbarItem
    {
        public ToolbarItem(IArtContext artContext)
        {
            ArtContext = artContext;
            Type = ToolbarItemType.Space;
        }

        public ToolbarItem(string name, ICommand command, string icon, IArtContext artContext)
        {
            Command = command;
            Name = name;
            Icon = icon;
            ArtContext = artContext;
            Type = ToolbarItemType.Button;
        }

        public IArtContext ArtContext { get; }

        public ICommand Command { get; }

        public string Icon { get; }

        public string Name { get; }

        public ToolbarItemType Type { get; }
    }

    public enum ToolbarItemType
    {
        Space  = 0,
        Button = 1
    }

    public enum MenuItemType
    {
        Separator = 0,
        Item      = 1
    }

    public static class ShortcutHelper
    {
        public static InputGesture GetGesture(string shortcut)
        {
            if (shortcut == null) return null;

            var gesture = shortcut.Split('+').Select(s => Prettify(s.Trim())).ToArray();
            var shift = gesture.Contains("Shift");
            var ctrl = gesture.Contains("Control");
            var alt = gesture.Contains("Alt");
            var key = gesture.Last();

            var kb = GetKeyboardLayout(0);
            var info = VkKeyScanEx(key.Trim()[0], kb);

            var vk = info & 0x00FF;
            var mod = (info & 0xFF00) >> 4;

            shift = shift || (mod & 1) == 1;
            ctrl = ctrl || (mod & 2) == 2;
            alt = alt || (mod & 4) == 4;

            var k = KeyInterop.KeyFromVirtualKey(vk);
            var m = ModifierKeys.None;

            if (shift) m |= ModifierKeys.Shift;
            if (ctrl) m |= ModifierKeys.Control;
            if (alt) m |= ModifierKeys.Alt;

            return new KeyGesture(k, m);
        }

        public static string Prettify(string raw)
        {
            if (raw == null) return null;

            IEnumerable<string> split = raw.Split('+');

            split = split.Replace("plus", "+");
            split = split.Replace("minus", "-");
            split = split.Replace("asterisk", "*");
            split = split.Replace("caret", "^");
            split = split.Replace("rightsquare", "]");
            split = split.Replace("leftsquare", "[");

            split = split.Replace("ctrl", "control");

            return Thread.CurrentThread.CurrentUICulture.TextInfo.ToTitleCase(
                string.Join(" + ", split));
        }

        [DllImport("user32.dll")]
        private static extern long GetKeyboardLayout(int thread);

        [DllImport("user32.dll")]
        private static extern short VkKeyScanEx(char tchar, long kb);
    }
}