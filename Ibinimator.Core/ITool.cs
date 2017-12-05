using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface ITool : INotifyPropertyChanged, IDisposable
    {
        string CursorImage { get; }
        float CursorRotate { get; }

        IToolManager Manager { get; }
        ToolOptions Options { get; }

        ToolType Type { get; }

        void Render(RenderContext target, ICacheManager cache, IViewManager view);

        #region Fill and Stroke

        /// <summary>
        ///     Applies the given brush to the current selection of the tool.
        /// </summary>
        /// <param name="brush">The brush to be applied.</param>
        void ApplyFill(IBrushInfo brush);

        /// <summary>
        ///     Applies the given stroke to the current selection of the tool.
        /// </summary>
        /// <param name="pen">The stroke to be applied.</param>
        void ApplyStroke(IPenInfo pen);

        /// <summary>
        ///     Queries the fill of the current selection of the tool.
        /// </summary>
        /// <returns>The current brush of the selection.</returns>
        IBrushInfo ProvideFill();

        /// <summary>
        ///     Queries the stroke of the current selection of the tool.
        /// </summary>
        /// <returns>The current stroke of the selection.</returns>
        IPenInfo ProvideStroke();

        #endregion

        #region Events

        bool KeyDown(Key key, ModifierKeys modifiers);
        bool KeyUp(Key key, ModifierKeys modifiers);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);

        bool TextInput(string text);

        #endregion
    }

    public class ToolOptions : INotifyCollectionChanged, IEnumerable<ToolOption>
    {
        private readonly Dictionary<string, ToolOption> _options = new Dictionary<string, ToolOption>();
        private bool _reentrancyFlag;

        public void Create(string id, string name)
        {
            var op = new ToolOption(id) {Name = name};
            op.PropertyChanged += OnOptionChanged;
            _options[id] = op;
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_reentrancyFlag) return;

            _reentrancyFlag = true;
            OptionChanged?.Invoke(sender, e);
            _reentrancyFlag = false;
        }

        public T Get<T>(string id)
        {
            return _options[id].Value is T t ? t : (T) Convert.ChangeType(_options[id], typeof(T));
        }
        
        public void SetMaximum(string id, float maximum) { _options[id].Maximum = maximum; }
        public void SetMinimum(string id, float minimum) { _options[id].Minimum = minimum; }
        public void SetType(string id, ToolOptionType type) { _options[id].Type = type; }
        public void SetValues(string id, IEnumerable<object> values)
        {
            _options[id].Values = values.ToArray();
        }

        #region IEnumerable<ToolOption> Members

        public IEnumerator<ToolOption> GetEnumerator() { return _options.Values.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        public event PropertyChangedEventHandler OptionChanged;
        public void SetUnit(string id, Unit unit) { _options[id].Unit = unit; }

        public void Set<T>(string id, T value) { _options[id].Value = value; }
    }

    public class ToolOption : Core.Model.Model
    {
        public ToolOption(string id) { Id = id; }

        public string Icon
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Id { get; }

        public float Maximum
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Minimum
        {
            get => Get<float>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
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

        public IEnumerable<object> Values
        {
            get => Get<IEnumerable<object>>();
            set => Set(value);
        }
    }
}