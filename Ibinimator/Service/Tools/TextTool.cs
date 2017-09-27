using System;
using System.Collections.Generic;
using Ibinimator.Utility;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service.Tools
{
    public sealed class TextTool : Model.Model, ITool
    {
        private readonly DW.FontCollection _dwFontCollection;

        private readonly Dictionary<string, (DW.FontStyle style, DW.FontStretch stretch, DW.FontWeight weight)>
            _fontFaceDescriptions = new Dictionary<string, (DW.FontStyle, DW.FontStretch, DW.FontWeight)>();

        private readonly ToolOption<string> _fontFamilyOption;

        private readonly ToolOption<string> _fontNameOption;
        private readonly ToolOption<float> _fontSizeOption;

        private bool _autoSet;

        private Vector2 _caretPosition;
        private Size2F _caretSize;
        private long _inputTime;
        private Vector2 _lastClickPos;

        private long _lastClickTime;
        private bool _mouseDown;

        private int _selectionIndex;
        private int _selectionRange;
        private RectangleF[] _selectionRects = new RectangleF[0];

        public TextTool(IToolManager manager)
        {
            Manager = manager;
            Status = "<b>Double Click</b> on canvas to create new text object. " +
                     "<b>Single Click</b> to select.";

            _dwFontCollection = Factory.GetSystemFontCollection(true);

            var fontNames = new List<string>();

            for (var i = 0; i < _dwFontCollection.FontFamilyCount; i++)
                using (var dwFontFamily = _dwFontCollection.GetFontFamily(i))
                {
                    fontNames.Add(dwFontFamily.FamilyNames.ToCurrentCulture());
                }

            fontNames.Sort();

            _fontFamilyOption = new ToolOption<string>("Font Family", ToolOptionType.Dropdown)
            {
                Options = fontNames.ToArray(),
                Value = fontNames.FirstOrDefault()
            };

            _fontFamilyOption.PropertyChanged += (s, e) =>
            {
                UpdateFontFaces();

                if (CurrentText == null || _fontFamilyOption.Value == null) return;

                if (_autoSet) return;

                if (_selectionRange == 0)
                    ArtView.HistoryManager.Do(new ApplyFormatCommand(
                        ArtView.HistoryManager.Position + 1,
                        new ITextLayer[] {CurrentText},
                        _fontFamilyOption.Value,
                        CurrentText.FontSize,
                        CurrentText.FontStretch,
                        CurrentText.FontStyle,
                        CurrentText.FontWeight,
                        new[] {CurrentText.FontFamilyName},
                        new[] {CurrentText.FontSize},
                        new[] {CurrentText.FontStretch},
                        new[] {CurrentText.FontStyle},
                        new[] {CurrentText.FontWeight}));
                else
                    Format(new Format
                    {
                        FontFamilyName = _fontFamilyOption.Value,
                        Range = new DW.TextRange(_selectionIndex, _selectionRange)
                    });

                Update();
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
                if (CurrentText == null) return;

                if (_autoSet) return;

                if (_selectionRange == 0)
                    ArtView.HistoryManager.Do(new ApplyFormatCommand(
                        ArtView.HistoryManager.Position + 1,
                        new ITextLayer[] {CurrentText},
                        CurrentText.FontFamilyName,
                        _fontSizeOption.Value,
                        CurrentText.FontStretch,
                        CurrentText.FontStyle,
                        CurrentText.FontWeight,
                        new[] {CurrentText.FontFamilyName},
                        new[] {CurrentText.FontSize},
                        new[] {CurrentText.FontStretch},
                        new[] {CurrentText.FontStyle},
                        new[] {CurrentText.FontWeight}));
                else
                    Format(new Format
                    {
                        FontSize = _fontSizeOption.Value,
                        Range = new DW.TextRange(_selectionIndex, _selectionRange)
                    });

                Update();
            };

            _fontNameOption = new ToolOption<string>("Font Face", ToolOptionType.Dropdown)
            {
                Value = "Regular"
            };

            _fontNameOption.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(ToolOption.Value)) return;

                if (CurrentText == null || _fontNameOption.Value == null) return;

                if (_autoSet) return;

                var (stretch, style, weight) = FromFontName(_fontNameOption.Value);

                if (_selectionRange == 0)
                    ArtView.HistoryManager.Do(new ApplyFormatCommand(
                        ArtView.HistoryManager.Position + 1,
                        new ITextLayer[] {CurrentText},
                        CurrentText.FontFamilyName,
                        CurrentText.FontSize,
                        stretch,
                        style,
                        weight,
                        new[] {CurrentText.FontFamilyName},
                        new[] {CurrentText.FontSize},
                        new[] {CurrentText.FontStretch},
                        new[] {CurrentText.FontStyle},
                        new[] {CurrentText.FontWeight}));
                else
                    Format(new Format
                    {
                        FontStretch = stretch,
                        FontStyle = style,
                        FontWeight = weight,
                        Range = new DW.TextRange(_selectionIndex, _selectionRange)
                    });

                Update();
            };

            Options = new ToolOption[]
            {
                _fontFamilyOption,
                _fontSizeOption,
                _fontNameOption
            };

            Manager.ArtView.SelectionManager.Updated += (_, e) => Update();
            Manager.ArtView.TextInput += ArtViewOnTextInput;

            Update();
        }

        public Text CurrentText => Manager.ArtView.SelectionManager.Selection.LastOrDefault() as Text;

        private ArtView ArtView => Manager.ArtView;

        private DW.Factory Factory => ArtView.DirectWriteFactory;

        private Layer Root => ArtView.ViewManager.Root;

        private void ArtViewOnTextInput(object sender, TextCompositionEventArgs e)
        {
            if (CurrentText == null) return;

            if (_selectionRange > 0)
                Remove(_selectionIndex, _selectionRange);

            Insert(_selectionIndex, e.Text);

            _selectionIndex += e.Text.Length;
            _selectionRange = 0;

            Update();

            _inputTime = Time.Now;
        }

        private void Format(Format format)
        {
            var history = ArtView.HistoryManager;

            var old = CurrentText.Formats.Select(f => f.Clone()).ToArray();
            CurrentText.SetFormat(format);
            var @new = CurrentText.Formats.Select(f => f.Clone()).ToArray();

            var current = new ApplyFormatRangeCommand(
                ArtView.HistoryManager.Position + 1,
                CurrentText, old, @new);

            // no Do() b/c it's already done
            history.Push(current);
        }

        private static (DW.FontStretch stretch, DW.FontStyle style, DW.FontWeight weight) FromFontName(string name)
        {
            var desc = name.Split(' ');
            var (stretch, style, weight) =
                (DW.FontStretch.Normal, DW.FontStyle.Normal, DW.FontWeight.Normal);

            foreach (var modifier in desc)
            {
                // all enums contain "Normal", so this would cause problems
                if (string.Equals("Normal", modifier, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (Enum.TryParse(modifier, true, out DW.FontStretch mStretch))
                    stretch = mStretch;

                if (Enum.TryParse(modifier, true, out DW.FontStyle mStyle))
                    style = mStyle;

                if (Enum.TryParse(modifier, true, out DW.FontWeight mWeight))
                    weight = mWeight;
            }

            return (stretch, style, weight);
        }

        [DllImport("user32.dll")]
        private static extern uint GetCaretBlinkTime();

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private void Insert(int index, string text)
        {
            var history = ArtView.HistoryManager;
            var current = new InsertTextCommand(
                ArtView.HistoryManager.Position + 1,
                CurrentText, text, index);

            history.Do(current);
        }

        private void Remove(int index, int length)
        {
            var history = ArtView.HistoryManager;
            var current = new RemoveTextCommand(
                ArtView.HistoryManager.Position + 1,
                CurrentText,
                CurrentText.Value.Substring(index, length),
                index);

            history.Do(current);
        }

        private string ToFontName(DW.FontWeight weight, DW.FontStyle style, DW.FontStretch stretch)
        {
            var str = "";
            if (weight != DW.FontWeight.Normal) str += weight + " ";
            if (style != DW.FontStyle.Normal) str += style + " ";
            if (stretch != DW.FontStretch.Normal) str += stretch + " ";
            return str == "" ? "Regular" : str.Trim();
        }

        private void Update()
        {
            if (CurrentText == null) return;

            _autoSet = true;

            var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

            var metrics = layout.HitTestTextPosition(
                _selectionIndex, false,
                out var _, out var _);

            _caretPosition = new Vector2(metrics.Left, metrics.Top);
            _caretSize = new Size2F((float) SystemParameters.CaretWidth, metrics.Height);

            if (_selectionRange > 0)
            {
                var rangeMetrics = layout.HitTestTextRange(_selectionIndex, _selectionRange, 0, 0);

                _selectionRects =
                    rangeMetrics
                        .Select(m => new RectangleF(m.Left, m.Top, m.Width, m.Height))
                        .ToArray();
            }
            else
            {
                _selectionRects = new RectangleF[0];
            }

            var format = CurrentText.GetFormat(_selectionIndex);

            _fontFamilyOption.Value = format?.FontFamilyName ?? CurrentText.FontFamilyName;
            _fontSizeOption.Value = format?.FontSize ?? CurrentText.FontSize;
            _fontNameOption.Value =
                ToFontName(
                    format?.FontWeight ?? CurrentText.FontWeight,
                    format?.FontStyle ?? CurrentText.FontStyle,
                    format?.FontStretch ?? CurrentText.FontStretch);

            _autoSet = false;

            Manager.ArtView.InvalidateSurface();
        }

        private void UpdateFontFaces()
        {
            var fontFaces = new List<string>();
            _fontFaceDescriptions.Clear();

            if (_dwFontCollection.FindFamilyName(_fontFamilyOption.Value, out var index))
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

            _fontNameOption.Options = fontFaces.ToArray();
        }

        #region ITool Members

        public void ApplyFill(BrushInfo brush)
        {
            if (CurrentText == null || brush == null) return;

            Format(new Format
            {
                Fill = brush,
                Range = new DW.TextRange(_selectionIndex, _selectionRange)
            });
        }

        public void ApplyStroke(BrushInfo brush, StrokeInfo stroke)
        {
            if (CurrentText == null || brush == null && stroke == null) return;

            Format(new Format
            {
                Stroke = brush,
                StrokeInfo = stroke,
                Range = new DW.TextRange(_selectionIndex, _selectionRange)
            });
        }

        public void Dispose()
        {
            _dwFontCollection?.Dispose();
            Cursor?.Dispose();

            Manager.ArtView.TextInput -= ArtViewOnTextInput;
        }

        public bool KeyDown(Key key)
        {
            if (CurrentText != null)
            {
                var mods = ArtView.Dispatcher.Invoke(() => Keyboard.Modifiers);
                var text = CurrentText.Value;

                Format format;
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
                    case Key.Down:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            var prev = text.Substring(0, _selectionIndex + _selectionRange)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selectionIndex + _selectionRange)
                                          .IndexOf("\n", StringComparison.Ordinal)
                                      + _selectionIndex + _selectionRange;

                            var next = text.Substring(end + 1)
                                           .IndexOf("\n", StringComparison.Ordinal)
                                       + end + 1;

                            var pos = _selectionIndex + _selectionRange - prev;

                            _selectionRange = Math.Min(next, end + pos) - _selectionIndex;
                        }
                        else
                        {
                            var prev = text
                                .Substring(0, _selectionIndex)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selectionIndex)
                                          .IndexOf("\n", StringComparison.Ordinal)
                                      + _selectionIndex;

                            var next = text.Substring(end + 1)
                                           .IndexOf("\n", StringComparison.Ordinal)
                                       + end + 1;

                            var pos = _selectionIndex - prev;

                            _selectionIndex = Math.Min(next, end + pos);
                            _selectionRange = 0;
                        }
                        break;
                    case Key.Up:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            var start = text.Substring(0, _selectionIndex)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var prev = text.Substring(0, start)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var pos = _selectionIndex - prev;

                            var selectionEnd = _selectionIndex + _selectionRange;
                            _selectionIndex = Math.Max(0, start + pos);
                            _selectionRange = selectionEnd - _selectionIndex;
                        }
                        else
                        {
                            var start = text.Substring(0, _selectionIndex)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var prev = text.Substring(0, start)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var pos = _selectionIndex - prev;

                            _selectionIndex = Math.Max(0, start + pos);
                            _selectionRange = 0;
                        }
                        break;
                    case Key.End:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            _selectionRange = text.Length - _selectionIndex;
                        }
                        else
                        {
                            _selectionIndex = text.Length;
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
                    case Key.Escape:
                        _selectionRange = 0;
                        break;

                    #endregion

                    #region Manipulation

                    case Key.Back:
                        if (_selectionIndex == 0 && _selectionRange == 0) break;

                        if (_selectionRange == 0)
                            Remove(--_selectionIndex, 1);
                        else
                            Remove(_selectionIndex, _selectionRange);

                        _selectionRange = 0;

                        _inputTime = Time.Now;
                        break;
                    case Key.Delete:
                        if (_selectionIndex + Math.Max(_selectionRange, 1) > text.Length) break;

                        Remove(_selectionIndex, Math.Max(_selectionRange, 1));

                        _selectionRange = 0;

                        _inputTime = Time.Now;
                        break;

                    #endregion

                    #region Shorcuts

                    case Key.A when mods.HasFlag(ModifierKeys.Control):
                        _selectionIndex = 0;
                        _selectionRange = CurrentText.Value.Length;
                        break;

                    case Key.B when mods.HasFlag(ModifierKeys.Control):
                        format = CurrentText.GetFormat(_selectionIndex);
                        var weight = format?.FontWeight ?? CurrentText.FontWeight;

                        Format(new Format
                        {
                            FontWeight = weight == DW.FontWeight.Normal ? DW.FontWeight.Bold : DW.FontWeight.Normal,
                            Range = new DW.TextRange(_selectionIndex, _selectionRange)
                        });
                        break;

                    case Key.I when mods.HasFlag(ModifierKeys.Control):
                        format = CurrentText.GetFormat(_selectionIndex);
                        var style = format?.FontStyle ?? CurrentText.FontStyle;

                        Format(new Format
                        {
                            FontStyle = style == DW.FontStyle.Normal ? DW.FontStyle.Italic : DW.FontStyle.Normal,
                            Range = new DW.TextRange(_selectionIndex, _selectionRange)
                        });
                        break;

                    case Key.C when mods.HasFlag(ModifierKeys.Control):
                        Clipboard.SetText(text.Substring(_selectionIndex, _selectionRange));
                        break;

                    case Key.X when mods.HasFlag(ModifierKeys.Control):
                        Clipboard.SetText(text.Substring(_selectionIndex, _selectionRange));
                        goto case Key.Back;

                    case Key.V when mods.HasFlag(ModifierKeys.Control):
                        if (_selectionRange > 0)
                            Remove(_selectionIndex, _selectionRange);

                        var pasted = Clipboard.GetText();

                        Insert(_selectionIndex, pasted);

                        _selectionRange = 0;
                        _selectionIndex += pasted.Length;
                        break;

                    #endregion

                    default:
                        return false;
                }

                _selectionIndex = MathUtils.Clamp(0, CurrentText.Value.Length, _selectionIndex);
                _selectionRange = MathUtils.Clamp(0, CurrentText.Value.Length - _selectionIndex, _selectionRange);

                Update();

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key)
        {
            return false;
        }

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

                    Update();
                }
            }

            return true;
        }

        public bool MouseUp(Vector2 pos)
        {
            _mouseDown = false;

            if (CurrentText == null)
            {
                if (Time.Now - _lastClickTime <= GetDoubleClickTime())
                {
                    var (stretch, style, weight) = FromFontName(_fontNameOption.Value);

                    var text = new Text
                    {
                        Position = pos,
                        FontFamilyName = _fontFamilyOption.Value,
                        FontSize = _fontSizeOption.Value,
                        FontStyle = style,
                        FontStretch = stretch,
                        FontWeight = weight,
                        FillBrush = ArtView.BrushManager.Fill,
                        StrokeBrush = ArtView.BrushManager.Stroke,
                        StrokeInfo = new StrokeInfo
                        {
                            Width = ArtView.BrushManager.StrokeWidth,
                            Style = ArtView.BrushManager.StrokeStyle
                        }
                    };

                    var root = ArtView.SelectionManager.Selection.OfType<Group>().LastOrDefault() ??
                               ArtView.SelectionManager.Root;

                    Manager.ArtView.HistoryManager.Do(
                        new AddLayerCommand(Manager.ArtView.HistoryManager.Position + 1,
                            root,
                            text));
                }

                _lastClickTime = Time.Now;

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
            else if (Time.Now - _lastClickTime <= GetDoubleClickTime())
            {
                // double click :D
                var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

                var metrics = layout.Metrics;

                var rect = new RectangleF(metrics.Top, metrics.Left, metrics.Width, metrics.Height);

                if (rect.Contains(tpos))
                {
                    var str = CurrentText.Value;
                    var start = _selectionIndex;
                    var end = start + _selectionRange;

                    while (start > 0 && !char.IsLetterOrDigit(str[start])) start--;
                    while (start > 0 && char.IsLetterOrDigit(str[start])) start--;

                    while (end < str.Length && !char.IsLetterOrDigit(str[end])) end++;
                    while (end < str.Length && char.IsLetterOrDigit(str[end])) end++;

                    _selectionIndex = start;
                    _selectionRange = end - start;
                    Update();
                }
            }
            else
            {
                _selectionRange = 0;

                var layout = ArtView.CacheManager.GetTextLayout(CurrentText);

                var metrics1 = layout.HitTestPoint(
                    tpos.X, tpos.Y,
                    out var isTrailingHit, out var _);

                _selectionIndex = metrics1.TextPosition + (isTrailingHit ? 1 : 0);
            }

            Update();

            _lastClickTime = Time.Now;

            return true;
        }

        public void Render(RenderTarget target, ICacheManager cacheManager)
        {
            if (CurrentText == null) return;

            target.Transform = CurrentText.AbsoluteTransform * target.Transform;

            if (_selectionRange == 0 && Time.Now % (GetCaretBlinkTime() * 2) < GetCaretBlinkTime())
                using (new StrokeStyle1(
                    target.Factory.QueryInterface<Factory1>(),
                    new StrokeStyleProperties1 {TransformType = StrokeTransformType.Fixed}))
                {
                    target.DrawLine(
                        _caretPosition,
                        new Vector2(
                            _caretPosition.X,
                            _caretPosition.Y + _caretSize.Height),
                        cacheManager.GetBrush("T2"),
                        _caretSize.Width / 2);
                }

            if (_selectionRange > 0)
                foreach (var selectionRect in _selectionRects)
                    target.FillRectangle(
                        selectionRect,
                        cacheManager.GetBrush("A1-1/2"));

            target.Transform = Matrix3x2.Invert(CurrentText.AbsoluteTransform) * target.Transform;

            ArtView.InvalidateSurface();
        }

        public Bitmap Cursor { get; private set; }

        public float CursorRotate { get; private set; }

        public IToolManager Manager { get; }

        public ToolOption[] Options { get; }

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public ToolType Type => ToolType.Text;

        #endregion
    }
}