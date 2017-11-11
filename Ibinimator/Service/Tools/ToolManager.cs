using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Tools
{
    public class ToolManager : Model, IToolManager
    {
        public ToolManager(ArtView artView, ISelectionManager selectionManager)
        {
            Context = artView;
        }

        public void SetTool(ToolType type)
        {
            lock (this)
            {
                var tool = Tool;
                Tool = null;
                tool?.Dispose();

                switch (type)
                {
                    case ToolType.Select:
                        Tool = new SelectTool(this, Context.SelectionManager);
                        break;
                    case ToolType.Node:
                        Tool = new NodeTool(this);
                        break;
                    case ToolType.Pencil:
                        Tool = new PencilTool(this);
                        break;
                    case ToolType.Pen:
                        break;
                    case ToolType.Eyedropper:
                        break;
                    case ToolType.Flood:
                        break;
                    case ToolType.Keyframe:
                        break;
                    case ToolType.Text:
                        Tool = new TextTool(this);
                        break;
                    case ToolType.Mask:
                        break;
                    case ToolType.Zoom:
                        break;
                    case ToolType.Gradient:
                        Tool = new GradientTool(this);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            RaisePropertyChanged(nameof(Type));
            Context.InvalidateSurface();
        }

        #region IToolManager Members

        public bool KeyDown(Key key, ModifierKeys modifiers)
        {
            lock (this)
            {
                return Tool?.KeyDown(key, modifiers) == true;
            }
        }

        public bool KeyUp(Key key, ModifierKeys modifiers)
        {
            lock (this)
            {
                return Tool?.KeyUp(key, modifiers) == true;
            }
        }

        public bool MouseDown(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseDown(pos) == true;
            }
        }

        public bool MouseMove(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseMove(pos) == true;
            }
        }

        public bool MouseUp(Vector2 pos)
        {
            lock (this)
            {
                return Tool?.MouseUp(pos) == true;
            }
        }

        public bool TextInput(string text)
        {
            lock (this)
            {
                return Tool?.TextInput(text) == true;
            }
        }

        public IArtContext Context { get; }

        public ITool Tool
        {
            get => Get<ITool>();
            private set
            {
                RaisePropertyChanged(nameof(Type));
                Set(value);
            }
        }

        public ToolType Type
        {
            get => Tool?.Type ?? ToolType.Select;
            set => SetTool(value);
        }

        #endregion
    }

    public enum ToolOptionType
    {
        Dropdown,
        Number,
        Switch,
        MultiSwitch
    }

    public class ToolOption : Model
    {
        public ImageSource Icon
        {
            get => Get<ImageSource>();
            set => Set(value);
        }

        public object Maximum
        {
            get => Get<object>();
            set => Set(value);
        }

        public object Minimum
        {
            get => Get<object>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public object[] Options
        {
            get => Get<object[]>();
            set => Set(value);
        }

        public ToolOptionType Type
        {
            get => Get<ToolOptionType>();
            set => Set(value);
        }

        public Unit Unit
        {
            get => Get<Unit>();
            set => Set(value);
        }

        public object Value
        {
            get => Get<object>();
            set => Set(value);
        }

        public void SetValue(object value)
        {
            SilentSet(value, nameof(Value));
        }
    }

    public class ToolOption<T> : ToolOption
    {
        public ToolOption(string name, ToolOptionType type)
        {
            Name = name;
            Type = type;
        }

        public new T Maximum
        {
            get => Get<T>();
            set => Set(value);
        }

        public new T Minimum
        {
            get => Get<T>();
            set => Set(value);
        }

        public new T[] Options
        {
            get => Get<T[]>();
            set => Set(value);
        }

        public new ToolOptionType Type
        {
            get => Get<ToolOptionType>();
            set => Set(value);
        }

        public new T Value
        {
            get => Get<T>();
            set => Set(Validate(value));
        }

        public void SetValue(T value)
        {
            SilentSet(value, nameof(Value));
        }

        private T Validate(T value)
        {
            switch (System.Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    var comparable = (IComparable<T>)value;

                    if (comparable.CompareTo(Minimum) < 0)
                        return Minimum;

                    if (comparable.CompareTo(Maximum) > 0)
                        return Maximum;

                    return value;
                default:
                    return value;
            }
        }
    }
}