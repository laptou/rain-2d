using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ibinimator.Renderer.Direct2D;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;
using DW = SharpDX.DirectWrite;
using FontStretch = Ibinimator.Core.Model.FontStretch;
using FontStyle = Ibinimator.Core.Model.FontStyle;
using FontWeight = Ibinimator.Core.Model.FontWeight;

namespace Ibinimator.Service.Tools
{
    public sealed class TextTool : Model, ITool
    {
        private readonly DW.Factory _dwFactory;
        private readonly DW.FontCollection _dwFontCollection;

        private (Vector2 position, Vector2 size) _caret;
        private (Vector2 position, bool down, long time) _mouse;
        private (int index, int length) _selection;

        private RectangleF[] _selectionRects = new RectangleF[0];

        public TextTool(IToolManager manager)
        {
            Manager = manager;
            Status = "<b>Double Click</b> on canvas to create new text object. " +
                     "<b>Single Click</b> to select.";

            _dwFactory = new DW.Factory(DW.FactoryType.Shared);
            _dwFontCollection = _dwFactory.GetSystemFontCollection(true);


            Options.Create("font-family", "Font Family");
            Options.SetType("font-family", ToolOptionType.Font);
            Options.SetValues("font-family",
                              Enumerable.Range(0, _dwFontCollection.FontFamilyCount)
                                        .Select(i =>
                                        {
                                            using (var dwFontFamily =
                                                _dwFontCollection.GetFontFamily(i))
                                                return dwFontFamily.FamilyNames
                                                                   .ToCurrentCulture();
                                        })
                                        .OrderBy(n => n));

            Options.Create("font-size", "Font Size");
            Options.SetValues("font-size",
                              new float[]
                              {
                                  8, 9, 10, 11, 12,
                                  14, 16, 18, 20, 22, 24, 28,
                                  32, 36, 40, 44, 48,
                                  72, 96, 120, 144, 288, 352
                              }.Cast<object>());
            Options.SetUnit("font-size", Unit.Points);
            Options.SetType("font-size", ToolOptionType.Length);
            Options.SetMinimum("font-size", 6);
            Options.SetMaximum("font-size", 60000);

            Options.Create("font-face", "Font Face");
            Options.SetValues("font-face", new[] {"Regular"});

            Options.OptionChanged += OnOptionChanged;

            Context.SelectionManager.Updated += OnSelectionUpdated;

            Update();
        }

        private void OnSelectionUpdated(object sender, EventArgs e) { Update(); }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var option = (ToolOption) sender;
            if (SelectedLayer == null) return;

            switch (option.Id)
            {
                case "font-family":
                    Options.SetValues("font-face", GetFontFaces());
                    goto case "font-face";
                case "font-face":

                    if (_selection.length == 0)
                        Format(new Format
                        {
                            FontFamilyName = (string) option.Value,
                            Range = (0, SelectedLayer.Value.Length)
                        });
                    else
                        Format(new Format
                        {
                            FontFamilyName = (string) option.Value,
                            Range = (_selection.index, _selection.length)
                        });
                    break;
                case "font-size":
                    if (_selection.length == 0)
                        Format(new Format
                        {
                            FontSize = (float) option.Value,
                            Range = (0, SelectedLayer.Value.Length)
                        });
                    else
                        Format(new Format
                        {
                            FontSize = (float) option.Value,
                            Range = (_selection.index, _selection.length)
                        });
                    break;
            }

            Update();
        }

        public Text SelectedLayer => Context.SelectionManager.Selection.LastOrDefault() as Text;

        public ToolOptions Options { get; } = new ToolOptions();

        private IArtContext Context => Manager.Context;

        private void Format(Format format)
        {
            var history = Context.HistoryManager;
            var cmd = new ApplyFormatCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer, format);

            // no Do() b/c it's already done
            history.Do(cmd);
        }

        private static (FontStretch stretch, FontStyle style, FontWeight weight) FromFontName(string name)
        {
            var desc = name.Split(' ');
            var (stretch, style, weight) =
                (FontStretch.Normal, FontStyle.Normal, FontWeight.Normal);

            foreach (var modifier in desc)
            {
                // all enums contain "Normal", so this would cause problems
                if (string.Equals("Normal", modifier, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (Enum.TryParse(modifier, true, out FontStretch mStretch))
                    stretch = mStretch;

                if (Enum.TryParse(modifier, true, out FontStyle mStyle))
                    style = mStyle;

                if (Enum.TryParse(modifier, true, out FontWeight mWeight))
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
            var history = Context.HistoryManager;
            var current = new InsertTextCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer, text, index);

            history.Do(current);
        }

        private void Remove(int index, int length)
        {
            var history = Context.HistoryManager;
            var current = new RemoveTextCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer,
                SelectedLayer.Value.Substring(index, length),
                index);

            history.Do(current);
        }

        private string ToFontName(FontWeight weight, FontStyle style, FontStretch stretch)
        {
            var str = "";
            if (weight != FontWeight.Normal) str += weight + " ";
            if (style != FontStyle.Normal) str += style + " ";
            if (stretch != FontStretch.Normal) str += stretch + " ";
            return str == "" ? "Regular" : str.Trim();
        }

        private void Update()
        {
            if (SelectedLayer == null) return;

            var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

            var metrics = layout.MeasurePosition(_selection.index);

            _caret.position = new Vector2(metrics.Left, metrics.Top);
            _caret.size = new Vector2((float) SystemParameters.CaretWidth, metrics.Height);

            _selectionRects = _selection.length > 0 ?
                layout.MeasureRange(_selection.index, _selection.length) :
                new RectangleF[0];

            var format = SelectedLayer.GetFormat(_selection.index);

            Options.Set("font-family", format?.FontFamilyName ?? SelectedLayer.FontFamilyName);
            Options.Set("font-size", format?.FontSize ?? SelectedLayer.FontSize);
            Options.Set("font-face", ToFontName(
                            format?.FontWeight ?? SelectedLayer.FontWeight,
                            format?.FontStyle ?? SelectedLayer.FontStyle,
                            format?.FontStretch ?? SelectedLayer.FontStretch));

            Context.InvalidateSurface();
        }

        private IEnumerable<string> GetFontFaces()
        {
            if (_dwFontCollection.FindFamilyName(Options.Get<string>("font-family"), out var index))
            {
                using (var dwFamily = _dwFontCollection.GetFontFamily(index))
                {
                    for (var i = 0; i < dwFamily.FontCount; i++)
                        using (var dwFont = dwFamily.GetFont(i))
                        {
                            yield return dwFont.FaceNames.ToCurrentCulture();
                        }
                }
            }
        }

        #region ITool Members

        public void ApplyFill(IBrushInfo brush)
        {
            if (SelectedLayer == null || brush == null) return;

            Format(new Format
            {
                Fill = brush,
                Range = (_selection.index, _selection.length)
            });
        }

        public void ApplyStroke(IPenInfo pen)
        {
            if (SelectedLayer == null || pen == null) return;

            Format(new Format
            {
                Stroke = pen,
                Range = _selection
            });
        }

        public IBrushInfo ProvideFill()
        {
            var format = SelectedLayer?.GetFormat(_selection.index);

            return format?.Fill ?? SelectedLayer?.Fill;
        }

        public IPenInfo ProvideStroke()
        {
            var format = SelectedLayer?.GetFormat(_selection.index);

            return format?.Stroke ?? SelectedLayer?.Stroke;
        }

        public void Dispose()
        {
            Context.SelectionManager.Updated -= OnSelectionUpdated;
            _dwFontCollection?.Dispose();
            _dwFactory?.Dispose();
        }

        public bool KeyDown(Key key, ModifierKeys mods)
        {
            if (SelectedLayer != null)
            {
                var text = SelectedLayer.Value;

                Format format;
                switch (key)
                {
                    #region Navigation

                    case Key.Left:
                        _selection.index--;

                        if (mods.HasFlag(ModifierKeys.Shift))
                            _selection.length++;
                        else
                            _selection.length = 0;
                        break;
                    case Key.Right:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            _selection.length++;
                        }
                        else
                        {
                            _selection.index++;
                            _selection.length = 0;
                        }
                        break;
                    case Key.Down:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            var prev = text.Substring(0, _selection.index + _selection.length)
                                           .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selection.index + _selection.length)
                                          .IndexOf("\n", StringComparison.Ordinal)
                                      + _selection.index + _selection.length;

                            var next = text.Substring(end + 1)
                                           .IndexOf("\n", StringComparison.Ordinal)
                                       + end + 1;

                            var pos = _selection.index + _selection.length - prev;

                            _selection.length = Math.Min(next, end + pos) - _selection.index;
                        }
                        else
                        {
                            var prev = text
                                .Substring(0, _selection.index)
                                .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selection.index)
                                          .IndexOf("\n", StringComparison.Ordinal)
                                      + _selection.index;

                            var next = text.Substring(end + 1)
                                           .IndexOf("\n", StringComparison.Ordinal)
                                       + end + 1;

                            var pos = _selection.index - prev;

                            _selection.index = Math.Min(next, end + pos);
                            _selection.length = 0;
                        }
                        break;
                    case Key.Up:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            var start = text.Substring(0, _selection.index)
                                            .LastIndexOf("\n", StringComparison.Ordinal);

                            var prev = text.Substring(0, start)
                                           .LastIndexOf("\n", StringComparison.Ordinal);

                            var pos = _selection.index - prev;

                            var selectionEnd = _selection.index + _selection.length;
                            _selection.index = Math.Max(0, start + pos);
                            _selection.length = selectionEnd - _selection.index;
                        }
                        else
                        {
                            var start = text.Substring(0, _selection.index)
                                            .LastIndexOf("\n", StringComparison.Ordinal);

                            var prev = text.Substring(0, start)
                                           .LastIndexOf("\n", StringComparison.Ordinal);

                            var pos = _selection.index - prev;

                            _selection.index = Math.Max(0, start + pos);
                            _selection.length = 0;
                        }
                        break;
                    case Key.End:
                        if (mods.HasFlag(ModifierKeys.Shift))
                        {
                            _selection.length = text.Length - _selection.index;
                        }
                        else
                        {
                            _selection.index = text.Length;
                            _selection.length = 0;
                        }
                        break;
                    case Key.Home:
                        if (mods.HasFlag(ModifierKeys.Shift))
                            _selection.length += _selection.index;
                        else
                            _selection.length = 0;

                        _selection.index = 0;
                        break;
                    case Key.Escape:
                        if (_selection.length == 0)
                            SelectedLayer.Selected = false;

                        _selection.length = 0;
                        break;

                    #endregion

                    #region Manipulation

                    case Key.Back:
                        if (_selection.index == 0 && _selection.length == 0) break;

                        if (_selection.length == 0)
                            Remove(--_selection.index, 1);
                        else
                            Remove(_selection.index, _selection.length);

                        _selection.length = 0;
                        break;
                    case Key.Delete:
                        if (_selection.index + Math.Max(_selection.length, 1) > text.Length) break;

                        Remove(_selection.index, Math.Max(_selection.length, 1));

                        _selection.length = 0;
                        break;

                    #endregion

                    #region Shorcuts

                    case Key.A when mods.HasFlag(ModifierKeys.Control):
                        _selection.index = 0;
                        _selection.length = SelectedLayer.Value.Length;
                        break;

                    case Key.B when mods.HasFlag(ModifierKeys.Control):
                        format = SelectedLayer.GetFormat(_selection.index);
                        var weight = format?.FontWeight ?? SelectedLayer.FontWeight;

                        Format(new Format
                        {
                            FontWeight = weight == FontWeight.Normal ? FontWeight.Bold : FontWeight.Normal,
                            Range = (_selection.index, _selection.length)
                        });
                        break;

                    case Key.I when mods.HasFlag(ModifierKeys.Control):
                        format = SelectedLayer.GetFormat(_selection.index);
                        var style = format?.FontStyle ?? SelectedLayer.FontStyle;

                        Format(new Format
                        {
                            FontStyle = style == FontStyle.Normal ? FontStyle.Italic : FontStyle.Normal,
                            Range = (_selection.index, _selection.length)
                        });
                        break;

                    case Key.C when mods.HasFlag(ModifierKeys.Control):
                        Clipboard.SetText(text.Substring(_selection.index, _selection.length));
                        break;

                    case Key.X when mods.HasFlag(ModifierKeys.Control):
                        Clipboard.SetText(text.Substring(_selection.index, _selection.length));
                        goto case Key.Back;

                    case Key.V when mods.HasFlag(ModifierKeys.Control):
                        if (_selection.length > 0)
                            Remove(_selection.index, _selection.length);

                        var pasted = App.Dispatcher.Invoke(Clipboard.GetText);

                        Insert(_selection.index, pasted);

                        _selection.length = 0;
                        _selection.index += pasted.Length;
                        break;

                    #endregion

                    default:
                        return false;
                }

                _selection.index = MathUtils.Clamp(0, SelectedLayer?.Value?.Length ?? 0, _selection.index);
                _selection.length = MathUtils.Clamp(0, SelectedLayer?.Value?.Length ?? 0 - _selection.index,
                                                    _selection.length);

                Update();

                return true;
            }

            return false;
        }

        public bool KeyUp(Key key, ModifierKeys modifiers) { return false; }

        public bool MouseDown(Vector2 pos)
        {
            _mouse.position = pos;
            _mouse.down = true;
            return false;
        }

        public bool MouseMove(Vector2 pos)
        {
            if (SelectedLayer == null) return false;

            if (_mouse.down)
            {
                var tlpos =
                    Vector2.Transform(_mouse.position, MathUtils.Invert(SelectedLayer.AbsoluteTransform));
                var tpos = Vector2.Transform(pos, MathUtils.Invert(SelectedLayer.AbsoluteTransform));

                if (!Context.CacheManager.GetBounds(SelectedLayer).Contains(tlpos))
                    return false;

                if (Vector2.Distance(tlpos, tpos) > 18)
                {
                    var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                    _selection.index = layout.GetPosition(tlpos, out var isTrailingHit) +
                                       (isTrailingHit ? 1 : 0);

                    var end = layout.GetPosition(tpos, out isTrailingHit) + (isTrailingHit ? 1 : 0);

                    _selection.length = Math.Abs(end - _selection.index);
                    _selection.index = Math.Min(_selection.index, end);

                    Update();
                }
            }

            return true;
        }

        public bool MouseUp(Vector2 pos)
        {
            _mouse.down = false;

            if (SelectedLayer == null)
            {
                if (Time.Now - _mouse.time <= GetDoubleClickTime())
                {
                    var (stretch, style, weight) = FromFontName(Options.Get<string>("font-face"));

                    var text = new Text
                    {
                        FontFamilyName = Options.Get<string>("font-family"),
                        FontSize = Options.Get<float>("font-size"),
                        FontStyle = style,
                        FontStretch = stretch,
                        FontWeight = weight,
                        Fill = Context.BrushManager.Fill,
                        Stroke = Context.BrushManager.Stroke
                    };
                    text.ApplyTransform(Matrix3x2.CreateTranslation(pos));

                    var root = Context.SelectionManager.Selection.OfType<Group>().LastOrDefault() ??
                               Context.ViewManager.Root;

                    Manager.Context.HistoryManager.Do(
                        new AddLayerCommand(Manager.Context.HistoryManager.Position + 1,
                                            root,
                                            text));

                    text.Selected = true;
                }

                _mouse.time = Time.Now;

                return false;
            }

            var tlpos = Vector2.Transform(_mouse.position, MathUtils.Invert(SelectedLayer.AbsoluteTransform));
            var tpos = Vector2.Transform(pos, MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            if (!Context.CacheManager.GetBounds(SelectedLayer).Contains(tlpos))
                return false;

            if (Vector2.Distance(tlpos, tpos) > 18)
            {
                // do nothing, this was handled in MouseMove()
                // but in this case we don't want to go into the 
                // else block
            }
            else if (Time.Now - _mouse.time <= GetDoubleClickTime())
            {
                // double click :D
                var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                var rect = layout.Measure();

                if (rect.Contains(tpos))
                {
                    var str = SelectedLayer.Value;
                    var start = _selection.index;
                    var end = start + _selection.length;

                    while (start > 0 && !char.IsLetterOrDigit(str[start])) start--;
                    while (start > 0 && char.IsLetterOrDigit(str[start])) start--;

                    while (end < str.Length && !char.IsLetterOrDigit(str[end])) end++;
                    while (end < str.Length && char.IsLetterOrDigit(str[end])) end++;

                    _selection.index = start;
                    _selection.length = end - start;
                    Update();
                }
            }
            else
            {
                _selection.length = 0;

                var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                var textPosition = layout.GetPosition(tpos, out var isTrailingHit);

                _selection.index = textPosition + (isTrailingHit ? 1 : 0);
            }

            Update();

            _mouse.time = Time.Now;

            return true;
        }

        public void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            if (SelectedLayer == null) return;

            target.Transform(SelectedLayer.AbsoluteTransform);

            if (_selection.length == 0 && Time.Now % (GetCaretBlinkTime() * 2) < GetCaretBlinkTime())
            {
                using (var pen =
                    target.CreatePen(_caret.size.X / 2, cache.GetBrush(nameof(EditorColors.TextCaret))))
                {
                    target.DrawLine(
                        _caret.position,
                        _caret.position + _caret.size,
                        pen);
                }
            }

            if (_selection.length > 0)
                foreach (var selectionRect in _selectionRects)
                    target.FillRectangle(
                        selectionRect,
                        cache.GetBrush(nameof(EditorColors.TextHighlight)));

            target.Transform(MathUtils.Invert(SelectedLayer.AbsoluteTransform));

            Context.InvalidateSurface();
        }

        public bool TextInput(string text)
        {
            if (SelectedLayer == null) return false;

            if (_selection.length > 0)
                Remove(_selection.index, _selection.length);

            Insert(_selection.index, text);

            _selection.index += text.Length;
            _selection.length = 0;

            Update();

            return true;
        }

        public string Cursor { get; private set; }

        public float CursorRotate { get; private set; }

        public IToolManager Manager { get; }

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public ToolType Type => ToolType.Text;

        #endregion
    }
}