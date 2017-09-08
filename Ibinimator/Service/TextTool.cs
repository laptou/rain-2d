using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Shared;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public sealed class TextTool : Model.Model, ITool
    {
        private SharpDX.DirectWrite.Factory Factory => ArtView.DirectWriteFactory;
        private readonly SharpDX.DirectWrite.FontCollection _dwFontCollection;

        private readonly Dictionary<string, (SharpDX.DirectWrite.FontStyle style, SharpDX.DirectWrite.FontStretch stretch, SharpDX.DirectWrite.FontWeight weight)>
            _fontFaceDescriptions = new Dictionary<string, (SharpDX.DirectWrite.FontStyle, SharpDX.DirectWrite.FontStretch, SharpDX.DirectWrite.FontWeight)>();

        private readonly ToolOption<string> _fontFaceOption;
        private readonly ToolOption<string> _fontNameOption;
        private readonly ToolOption<float> _fontSizeOption;

        private Vector2 _caretPosition;
        private Size2F _caretSize;

        private long _lastClickTime;
        private Vector2 _lastClickPos;
        private bool _mouseDown;

        private int _selectionIndex;
        private int _selectionRange;
        private Size2F _selectionSize;

        public TextTool(IToolManager manager)
        {
            Manager = manager;
            Manager.ArtView.SelectionManager.Updated += (_, e) => Update();
            Manager.ArtView.TextInput += ArtViewOnTextInput;
            
            _dwFontCollection = Factory.GetSystemFontCollection(true);

            var fontNames = new List<string>();

            for (var i = 0; i < _dwFontCollection.FontFamilyCount; i++)
                using (var dwFontFamily = _dwFontCollection.GetFontFamily(i))
                    fontNames.Add(dwFontFamily.FamilyNames.ToCurrentCulture());

            fontNames.Sort();

            _fontNameOption = new ToolOption<string>("Font Family", ToolOptionType.Dropdown)
            {
                Options = fontNames.ToArray(),
                Value = fontNames.FirstOrDefault()
            };

            _fontNameOption.PropertyChanged += (s, e) =>
            {
                var fontFaces = new List<string>();
                _fontFaceDescriptions.Clear();

                if (_dwFontCollection.FindFamilyName(_fontNameOption.Value, out var index))
                {
                    using (var dwFamily = _dwFontCollection.GetFontFamily(index))
                    {
                        for (var i = 0; i < dwFamily.FontCount; i++)
                            using (var dwFont = dwFamily.GetFont(i))
                            {
                                var name = dwFont.FaceNames.ToCurrentCulture();
                                fontFaces.Add(name);
                                _fontFaceDescriptions[name] =
                                    (dwFont.Style, dwFont.Stretch, dwFont.Weight);
                            }
                    }
                }

                _fontFaceOption.Options = fontFaces.ToArray();

                if (CurrentText != null && _fontNameOption.Value != null)
                {
                    if(_selectionRange == 0)
                        CurrentText.FontFamilyName = _fontNameOption.Value;
                    else
                        CurrentText.SetFormat(new Text.Format
                        {
                            FontFamilyName = _fontNameOption.Value,
                            Range = new SharpDX.DirectWrite.TextRange(_selectionIndex, _selectionRange)
                        });
                }
            };

            _fontSizeOption = new ToolOption<float>("Font Size", ToolOptionType.Number)
            {
                Options = new float[]
                {
                    8, 9, 10, 11, 12,
                    14, 16, 18, 20, 22, 24, 28,
                    32, 36, 40, 44, 48,
                    72, 96, 120, 144, 288, 352
                },
                Minimum = 1,
                Maximum = 10000,
                Unit = Unit.Points,
                Value = 12
            };

            _fontSizeOption.PropertyChanged += (s, e) =>
            {
                if (CurrentText != null)
                {
                    if (_selectionRange == 0)
                        CurrentText.FontSize = _fontSizeOption.Value;
                    else
                        CurrentText.SetFormat(new Text.Format
                        {
                            FontSize = _fontSizeOption.Value,
                            Range = new SharpDX.DirectWrite.TextRange(_selectionIndex, _selectionRange)
                        });
                }
            };

            _fontFaceOption = new ToolOption<string>("Font Face", ToolOptionType.Dropdown);

            _fontFaceOption.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ToolOption.Value))
                {
                    if (CurrentText != null && _fontFaceOption.Value != null)
                    {
                        var desc = _fontFaceDescriptions[_fontFaceOption.Value];
                        
                        if (_selectionRange == 0)
                        {
                            CurrentText.FontStretch = desc.stretch;
                            CurrentText.FontStyle = desc.style;
                            CurrentText.FontWeight = desc.weight;
                        }
                        else
                        {
                            CurrentText.SetFormat(new Text.Format
                            {
                                FontStretch = desc.stretch,
                                FontStyle = desc.style,
                                FontWeight = desc.weight,
                                Range = new SharpDX.DirectWrite.TextRange(_selectionIndex, _selectionRange)
                            });
                        }
                    }
                }
            };

            Options = new ToolOption[]
            {
                _fontNameOption, _fontSizeOption, _fontFaceOption
            };

            Update();
            UpdateCaret();
        }

        public Text CurrentText => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Text;

        private ArtView ArtView => Manager.ArtView;

        private long Now => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        private Layer Root => ArtView.ViewManager.Root;

        #region ITool Members

        public Bitmap Cursor { get; private set; }

        public float CursorRotate { get; private set; }

        public bool KeyDown(Key key)
        {
            if (CurrentText != null)
            {
                var mods = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);

                switch (key)
                {
                    #region Navigation

                    case Key.Left:
                        _selectionIndex--;

                        if (mods.HasFlag(ModifierKeys.Shift))
                            _selectionRange++;
                        else
                            _selectionRange = 0;
                        break;
                    case Key.Right:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            _selectionRange++;
                        }
                        else
                        {
                            _selectionIndex++;
                            _selectionRange = 0;
                        }
                        break;
                    case Key.End:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            _selectionRange = CurrentText.Value.Length - _selectionIndex;
                        }
                        else
                        {
                            _selectionIndex = CurrentText.Value.Length;
                            _selectionRange = 0;
                        }
                        break;
                    case Key.Home:
                        if (mods.HasFlag(ModifierKeys.Shift))
                            _selectionRange += _selectionIndex;
                        else
                            _selectionRange = 0;

                        _selectionIndex = 0;
                        break;

                    #endregion

                    #region Manipulation

                    case Key.Back:
                        if (_selectionRange == 0)
                            CurrentText.Value = CurrentText.Value.Remove(--_selectionIndex, 1);
                        else
                            CurrentText.Value = CurrentText.Value.Remove(_selectionIndex, _selectionRange);

                        _selectionRange = 0;
                        break;
                    case Key.Delete:
                        if (_selectionRange == 0)
                            CurrentText.Value = CurrentText.Value.Remove(_selectionIndex, 1);
                        else
                            CurrentText.Value = CurrentText.Value.Remove(_selectionIndex, _selectionRange);

                        _selectionRange = 0;
                        break;

                    #endregion

                    #region Shorcuts

                    case Key.A when mods.HasFlag(ModifierKeys.Control):
                        _selectionIndex = 0;
                        _selectionRange = CurrentText.Value.Length;
                        break;

                    #endregion

                    default:
                        return false;
                }

                _selectionIndex = MathUtils.Clamp(0, CurrentText.Value.Length, _selectionIndex);
                _selectionRange = MathUtils.Clamp(0, CurrentText.Value.Length - _selectionIndex, _selectionRange);

                UpdateCaret();

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key)
        {
            return false;
        }

        public IToolManager Manager { get; }

        public bool MouseDown(Vector2 pos)
        {
            _lastClickPos = pos;
            _mouseDown = true;
            return false;
        }

        public bool MouseMove(Vector2 pos)
        {
            if (CurrentText == null) return false;

            if (_mouseDown)
            {
                var tlpos = Matrix3x2.TransformPoint(Matrix3x2.Invert(CurrentText.AbsoluteTransform), _lastClickPos);
                var tpos = Matrix3x2.TransformPoint(Matrix3x2.Invert(CurrentText.AbsoluteTransform), pos);

                if (!ArtView.CacheManager.GetBounds(CurrentText).Contains(tlpos))
                    return false;

                if (Vector2.Distance(tlpos, tpos) > 18)
                {
                    var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

                    layout.GetLineMetrics();

                    var metrics1 = layout.HitTestPoint(
                        tlpos.X, tlpos.Y,
                        out var isTrailingHit, out var _);

                    _selectionIndex = metrics1.TextPosition + (isTrailingHit ? 1 : 0);

                    var metrics2 = layout.HitTestPoint(
                        tpos.X, tpos.Y,
                        out isTrailingHit, out var _);

                    var end = metrics2.TextPosition + (isTrailingHit ? 1 : 0);

                    _selectionRange = Math.Abs(end - _selectionIndex);
                    _selectionIndex = Math.Min(_selectionIndex, end);

                    UpdateCaret();
                }
            }

            return true;
        }

        public bool MouseUp(Vector2 pos)
        {
            _mouseDown = false;

            if (CurrentText == null)
            {
                _lastClickTime = Now;
                return false;
            }

            var tlpos = Matrix3x2.TransformPoint(Matrix3x2.Invert(CurrentText.AbsoluteTransform), _lastClickPos);
            var tpos = Matrix3x2.TransformPoint(Matrix3x2.Invert(CurrentText.AbsoluteTransform), pos);

            if (!ArtView.CacheManager.GetBounds(CurrentText).Contains(tlpos))
                return false;

            if (Vector2.Distance(tlpos, tpos) > 18)
            {
                // do nothing, this was handled in MouseMove()
                // but in this case we don't want to go into the 
                // else block
            }
            else if (Now - _lastClickTime <= GetDoubleClickTime())
            {
                var selectionRect = new RectangleF(
                    _caretPosition.X,
                    _caretPosition.Y,
                    _selectionSize.Width,
                    _selectionSize.Height);

                if (selectionRect.Contains(tpos) || _selectionRange == 0)
                {
                    // double click :D
                    var str = CurrentText.Value;
                    var start = _selectionIndex;
                    var end = start + _selectionRange;

                    while (start > 0 && !char.IsLetterOrDigit(str[start])) start--;
                    while (start > 0 && char.IsLetterOrDigit(str[start])) start--;

                    while (end < str.Length && !char.IsLetterOrDigit(str[end])) end++;
                    while (end < str.Length && char.IsLetterOrDigit(str[end])) end++;

                    _selectionIndex = start;
                    _selectionRange = end - start;
                    UpdateCaret();
                }
            }
            else
            {
                _selectionRange = 0;

                var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

                layout.GetLineMetrics();

                var metrics1 = layout.HitTestPoint(
                    tpos.X, tpos.Y,
                    out var isTrailingHit, out var _);

                _selectionIndex = metrics1.TextPosition + (isTrailingHit ? 1 : 0);
            }

            UpdateCaret();

            _lastClickTime = Now;

            return true;
        }

        public ToolOption[] Options { get; }

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            if (CurrentText != null)
            {
                target.Transform = CurrentText.AbsoluteTransform * target.Transform;

                if (DateTime.Now.Millisecond < 500)
                    target.FillRectangle(
                        new RectangleF(
                            _caretPosition.X,
                            _caretPosition.Y,
                            _caretSize.Width,
                            _caretSize.Height),
                        cacheManager.GetBrush("T2"));

                if (_selectionRange > 0)
                    target.FillRectangle(
                        new RectangleF(
                            _caretPosition.X,
                            _caretPosition.Y,
                            _selectionSize.Width,
                            _selectionSize.Height),
                        cacheManager.GetBrush("A1-1/2"));

                target.Transform = Matrix3x2.Invert(CurrentText.AbsoluteTransform) * target.Transform;

                ArtView.InvalidateSurface();
            }
        }

        public string Status { get; }

        public ToolType Type => ToolType.Text;

        #endregion

        private void ArtViewOnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (CurrentText == null) return;

            if(_selectionRange > 0)
                CurrentText.Value = CurrentText.Value.Remove(_selectionIndex, _selectionRange);

            CurrentText.Value = CurrentText.Value.Insert(_selectionIndex, e.Text);

            _selectionIndex += e.Text.Length;
            _selectionRange = 0;

            UpdateCaret();
        }

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private void Update()
        {
            if (CurrentText == null) return;

            _fontNameOption.Value = CurrentText.FontFamilyName;
            _fontSizeOption.Value = CurrentText.FontSize;
            _fontFaceOption.Value = _fontFaceDescriptions
                .FirstOrDefault(kv =>
                    kv.Value.style == CurrentText.FontStyle &&
                    kv.Value.stretch == CurrentText.FontStretch &&
                    kv.Value.weight == CurrentText.FontWeight)
                .Key;

            UpdateCaret();
        }

        private void UpdateCaret()
        {
            if (CurrentText == null) return;

            var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

            var metrics = layout.HitTestTextPosition(
                _selectionIndex, false,
                out var _, out var _);

            _caretPosition = new Vector2(metrics.Left, metrics.Top);
            _caretSize = new Size2F((float) SystemParameters.CaretWidth, metrics.Height);

            if (_selectionRange > 0)
            {
                var rangeMetrics = layout.HitTestTextRange(_selectionIndex, _selectionRange, 0, 0);
                var area = rangeMetrics
                    .Select(m => new RectangleF(m.Left, m.Top, m.Width, m.Height))
                    .Aggregate(RectangleF.Union);

                _selectionSize = area.Size;
            }
            else
            {
                _selectionSize = Size2F.Empty;
            }

            Manager.ArtView.InvalidateSurface();
        }

        public void Dispose()
        {
            _dwFontCollection?.Dispose();
            Cursor?.Dispose();

            Manager.ArtView.TextInput -= ArtViewOnTextInput;
        }
    }
}