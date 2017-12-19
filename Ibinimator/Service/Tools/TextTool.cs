using System;
using System.Collections.Generic;
using System.ComponentModel;

using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Direct2D;

using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;
using Ibinimator.Resources;
using Ibinimator.Service.Commands;

using DW = SharpDX.DirectWrite;
using FontStretch = Ibinimator.Core.Model.FontStretch;
using FontStyle = Ibinimator.Core.Model.FontStyle;
using FontWeight = Ibinimator.Core.Model.FontWeight;

namespace Ibinimator.Service.Tools
{
    public sealed class TextTool : SelectionToolBase
    {
        private readonly DW.Factory        _dwFactory;
        private readonly DW.FontCollection _dwFontCollection;

        private (Vector2 position, Vector2 size, bool visible) _caret;
        private (Vector2 start, Vector2 end, long time)        _drag;

        private (Vector2 position, bool down, long time, long previousTime) _mouse;
        private (int index, int length)                                     _selection;

        private RectangleF[] _selectionRects = new RectangleF[0];
        private bool         _updatingOptions;

        public TextTool(IToolManager manager, ISelectionManager selectionManager)
            : base(manager, selectionManager)
        {
            Type = ToolType.Text;
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
                                                    {
                                                        return dwFontFamily.FamilyNames
                                                                           .ToCurrentCulture();
                                                    }
                                                })
                                        .OrderBy(n => n));

            using (var defaultFont = _dwFontCollection.GetFontFamily(0))
            {
                Options.Set("font-family", defaultFont.FamilyNames.ToCurrentCulture());
            }

            Options.Create("font-size", "Font Size");
            Options.SetValues("font-size",
                              new float[]
                              {
                                  8, 9, 10, 11, 12,
                                  14, 16, 18, 20, 22, 24, 28,
                                  32, 36, 40, 44, 48,
                                  72, 96, 120, 144, 288, 352
                              });
            Options.SetUnit("font-size", Unit.Points);
            Options.SetType("font-size", ToolOptionType.Length);
            Options.SetMinimum("font-size", 6);
            Options.SetMaximum("font-size", 60000);
            Options.Set("font-size", 12);

            Options.Create("font-stretch", "Stretch");
            Options.Set("font-stretch", FontStretch.Normal);
            Options.SetType("font-stretch", ToolOptionType.Dropdown);
            Options.SetValues("font-stretch", new[]
            {
                FontStretch.Normal
            });

            Options.Create("font-weight", "Weight");
            Options.Set("font-weight", FontWeight.Normal);
            Options.SetType("font-weight", ToolOptionType.Dropdown);
            Options.SetValues("font-weight", new[]
            {
                FontWeight.Normal
            });

            Options.Create("font-style", "Style");
            Options.Set("font-style", FontStyle.Normal);
            Options.SetType("font-style", ToolOptionType.Dropdown);
            Options.SetValues("font-style", new[]
            {
                FontStyle.Normal
            });

            Options.OptionChanged += OnOptionChanged;

            Update();
        }

        public Text SelectedLayer => Selection.LastOrDefault() as Text;

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public override void ApplyFill(IBrushInfo brush)
        {
            if (SelectedLayer == null || brush == null) return;

            Format(new Format
            {
                Fill = brush,
                Range = (_selection.index, _selection.length)
            }, true);
        }

        public override void ApplyStroke(IPenInfo pen)
        {
            if (SelectedLayer == null || pen == null) return;

            Format(new Format
            {
                Stroke = pen,
                Range = _selection
            }, true);
        }

        public override void Dispose()
        {
            base.Dispose();
            _dwFontCollection?.Dispose();
            _dwFactory?.Dispose();
        }

        public Vector2 FromWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectedLayer.AbsoluteTransform));
        }

        public override bool KeyDown(Key key, ModifierState mods)
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

                        if (mods.Shift)
                            _selection.length++;
                        else
                            _selection.length = 0;

                        break;
                    case Key.Right:

                        if (mods.Shift)
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

                        if (mods.Shift)
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

                        if (mods.Shift)
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

                        if (mods.Shift)
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
                        if (mods.Shift)
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

                    case Key.Return:
                        TextInput(Environment.NewLine);

                        break;

                    #endregion

                    #region Shorcuts

                    case Key.A when mods.Control:
                        _selection.index = 0;
                        _selection.length = SelectedLayer.Value.Length;

                        break;

                    case Key.B when mods.Control:
                        format = SelectedLayer.GetFormat(_selection.index);
                        var weight = format?.FontWeight ?? SelectedLayer.FontWeight;

                        Format(new Format
                        {
                            FontWeight = weight == FontWeight.Normal ? FontWeight.Bold : FontWeight.Normal,
                            Range = (_selection.index, _selection.length)
                        });

                        break;

                    case Key.I when mods.Control:
                        format = SelectedLayer.GetFormat(_selection.index);
                        var style = format?.FontStyle ?? SelectedLayer.FontStyle;

                        Format(new Format
                        {
                            FontStyle = style == FontStyle.Normal ? FontStyle.Italic : FontStyle.Normal,
                            Range = (_selection.index, _selection.length)
                        });

                        break;

                    case Key.C when mods.Control:
                        Clipboard.SetText(text.Substring(_selection.index, _selection.length));

                        break;

                    case Key.X when mods.Control:
                        Clipboard.SetText(text.Substring(_selection.index, _selection.length));
                        goto case Key.Back;

                    case Key.V when mods.Control:
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

        public override bool KeyUp(Key key, ModifierState modifiers) { return false; }

        public override bool MouseDown(Vector2 pos, ModifierState state)
        {
            _mouse = (pos, true, Time.Now, _mouse.time);
            _drag = (pos, pos, Time.Now);

            return base.MouseDown(pos, state);
        }

        public override bool MouseMove(Vector2 pos, ModifierState state)
        {
            if (SelectedLayer == null)
            {
                _mouse.position = pos;

                return false;
            }

            _drag.end = pos;

            var bounds = Context.CacheManager.GetAbsoluteBounds(SelectedLayer);

            if (_mouse.down && bounds.Contains(_drag.start))
            {
                var start = FromWorldSpace(_drag.start);
                var end = FromWorldSpace(_drag.end);

                var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                var selectionStart = layout.GetPosition(start, out var isTrailingHit) +
                                     (isTrailingHit ? 1 : 0);

                var selectionEnd = layout.GetPosition(end, out isTrailingHit) + (isTrailingHit ? 1 : 0);

                _selection.index = Math.Min(selectionStart, selectionEnd);
                _selection.length = Math.Max(selectionStart, selectionEnd) - _selection.index;

                Update();
            }

            _mouse.position = pos;

            return true;
        }

        public override bool MouseUp(Vector2 pos, ModifierState state)
        {
            _mouse.down = false;
            _drag.end = pos;

            if (SelectedLayer == null)
            {
                if (_mouse.time - _mouse.previousTime > Time.DoubleClick)
                    return base.MouseUp(pos, state);

                // if double click, make a new text object
                var text = new Text
                {
                    FontFamilyName = Options.Get<string>("font-family"),
                    FontSize = Options.Get<float>("font-size"),
                    FontStyle = Options.Get<FontStyle>("font-style"),
                    FontStretch = Options.Get<FontStretch>("font-stretch"),
                    FontWeight = Options.Get<FontWeight>("font-weight"),
                    Fill = Context.BrushManager.BrushHistory.FirstOrDefault()
                };
                text.ApplyTransform(Matrix3x2.CreateTranslation(pos));

                var root = Context.SelectionManager.Selection.OfType<Group>().LastOrDefault() ??
                           Context.ViewManager.Root;

                Manager.Context.HistoryManager.Do(
                    new AddLayerCommand(Manager.Context.HistoryManager.Position + 1,
                                        root,
                                        text));

                text.Selected = true;

                return true;
            }

            var tpos = FromWorldSpace(pos);

            if (!Context.CacheManager.GetBounds(SelectedLayer).Contains(tpos))
                return base.MouseUp(pos, state);

            if (_mouse.time - _mouse.previousTime <= Time.DoubleClick)
            {
                // double click, select more
                var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                var rect = layout.Measure();

                if (rect.Contains(tpos))
                {
                    var str = SelectedLayer.Value;
                    var start = _selection.index;
                    var end = start + _selection.length;

                    // move backwards until we encounter an alphanumeric character
                    while (start > 0 && !char.IsLetterOrDigit(str[start])) start--;

                    // continue moving backwards a non-alphanumeric character (word boundary)
                    while (start > 0 && char.IsLetterOrDigit(str[start])) start--;

                    while (end < str.Length && !char.IsLetterOrDigit(str[end])) end++;
                    while (end < str.Length && char.IsLetterOrDigit(str[end])) end++;

                    _selection.index = start;
                    _selection.length = end - start;
                    Update();
                }
            }
            else if (_drag.start == _drag.end)
            {
                _selection.length = 0;

                var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

                var textPosition = layout.GetPosition(tpos, out var isTrailingHit);

                _selection.index = textPosition + (isTrailingHit ? 1 : 0);
            }

            Update();

            return true;
        }

        public override IBrushInfo ProvideFill()
        {
            var format = SelectedLayer?.GetFormat(_selection.index);

            return format?.Fill ?? base.ProvideFill();
        }

        public override IPenInfo ProvideStroke()
        {
            var format = SelectedLayer?.GetFormat(_selection.index);

            return format?.Stroke ?? base.ProvideStroke();
        }

        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            if (SelectedLayer == null) return;

            RenderBoundingBox(target, cache, view);

            target.Transform(SelectedLayer.AbsoluteTransform);

            if (_selection.length == 0 && Time.Now % (GetCaretBlinkTime() * 2) < GetCaretBlinkTime())

                target.FillRectangle(
                    _caret.position.X,
                    _caret.position.Y,
                    _caret.size.X,
                    _caret.size.Y,
                    cache.GetBrush(nameof(EditorColors.TextCaret)));

            if (_selection.length > 0)
                foreach (var selectionRect in _selectionRects)
                    target.FillRectangle(
                        selectionRect,
                        cache.GetBrush(nameof(EditorColors.TextHighlight)));

            target.Transform(MathUtils.Invert(SelectedLayer.AbsoluteTransform));
        }

        public override bool TextInput(string text)
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

        public Vector2 ToWorldSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectedLayer.AbsoluteTransform);
        }

        protected override void OnSelectionUpdated(object sender, EventArgs e) { Update(); }

        private void Format(Format format, bool merge = false)
        {
            var history = Context.HistoryManager;
            var cmd = new ApplyFormatCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer, format);

            if (merge)

                history.Merge(cmd, Time.DoubleClick);
            else history.Do(cmd);
        }

        [DllImport("user32.dll")]
        private static extern uint GetCaretBlinkTime();

        private IEnumerable<FontFace> GetFontFaces()
        {
            if (!_dwFontCollection.FindFamilyName(Options.Get<string>("font-family"), out var index))
                yield break;

            using (var dwFamily = _dwFontCollection.GetFontFamily(index))
            {
                for (var i = 0; i < dwFamily.FontCount; i++)
                    using (var dwFont = dwFamily.GetFont(i))
                    {
                        yield return new FontFace((FontStretch) dwFont.Stretch,
                                                  (FontStyle) dwFont.Style,
                                                  (FontWeight) dwFont.Weight);
                    }
            }
        }


        private void Insert(int index, string text)
        {
            if (text.Length == 0) return;

            var history = Context.HistoryManager;
            var current = new InsertTextCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer, text, index);

            history.Do(current);
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_updatingOptions) return;

            var option = (ToolOption) sender;

            if (SelectedLayer == null) return;
            if (!(e.PropertyName == nameof(ToolOption.Value) ||
                  e.PropertyName == nameof(ToolOption.Values)))
                return;

            var updated = false;

            switch (option.Id)
            {
                case "font-family":
                    UpdateOptions(Options.Get<string>("font-family"),
                                  Options.Get<float>("font-size"),
                                  Options.Get<FontStretch>("font-stretch"),
                                  Options.Get<FontWeight>("font-weight"),
                                  Options.Get<FontStyle>("font-style"));
                    updated = true;

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
                    goto case "font-stretch";

                case "font-stretch":

                    if (!updated)
                    {
                        UpdateOptions(Options.Get<string>("font-family"),
                                      Options.Get<float>("font-size"),
                                      Options.Get<FontStretch>("font-stretch"),
                                      Options.Get<FontWeight>("font-weight"),
                                      Options.Get<FontStyle>("font-style"));
                        updated = true;
                    }

                    var stretch = Options.Get<FontStretch>("font-stretch");
                    if (_selection.length == 0)
                        Format(new Format
                        {
                            FontStretch = stretch,
                            Range = (0, SelectedLayer.Value.Length)
                        });
                    else
                        Format(new Format
                        {
                            FontStretch = stretch,
                            Range = (_selection.index, _selection.length)
                        });
                    goto case "font-weight";

                case "font-weight":

                    if (!updated)
                    {
                        UpdateOptions(Options.Get<string>("font-family"),
                                      Options.Get<float>("font-size"),
                                      Options.Get<FontStretch>("font-stretch"),
                                      Options.Get<FontWeight>("font-weight"),
                                      Options.Get<FontStyle>("font-style"));
                        updated = true;
                    }

                    var weight = Options.Get<FontWeight>("font-weight");
                    if (_selection.length == 0)
                        Format(new Format
                        {
                            FontWeight = weight,
                            Range = (0, SelectedLayer.Value.Length)
                        });
                    else
                        Format(new Format
                        {
                            FontWeight = weight,
                            Range = (_selection.index, _selection.length)
                        });


                    goto case "font-style";

                case "font-style":
                    if (!updated)
                        UpdateOptions(Options.Get<string>("font-family"),
                                      Options.Get<float>("font-size"),
                                      Options.Get<FontStretch>("font-stretch"),
                                      Options.Get<FontWeight>("font-weight"),
                                      Options.Get<FontStyle>("font-style"));

                    var style = Options.Get<FontStyle>("font-style");
                    if (_selection.length == 0)
                        Format(new Format
                        {
                            FontStyle = style,
                            Range = (0, SelectedLayer.Value.Length)
                        });
                    else
                        Format(new Format
                        {
                            FontStyle = style,
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

        private void Remove(int index, int length)
        {
            if (length == 0) return;

            var history = Context.HistoryManager;
            var current = new RemoveTextCommand(
                Context.HistoryManager.Position + 1,
                SelectedLayer,
                SelectedLayer.Value.Substring(index, length),
                index);

            history.Do(current);
        }

        private void Update()
        {
            if (SelectedLayer == null) return;

            // if (!_caret.visible) 

            var layout = Context.CacheManager.GetTextLayout(SelectedLayer);

            var metrics = layout.MeasurePosition(_selection.index);

            _caret.position = new Vector2(metrics.Left, metrics.Top);
            _caret.size = new Vector2((float) SystemParameters.CaretWidth, metrics.Height);

            _selectionRects = _selection.length > 0 ?
                                  layout.MeasureRange(_selection.index, _selection.length) :
                                  new RectangleF[0];

            var format = SelectedLayer.GetFormat(_selection.index);

            UpdateOptions(format?.FontFamilyName ?? SelectedLayer.FontFamilyName,
                          format?.FontSize ?? SelectedLayer.FontSize,
                          format?.FontStretch ?? SelectedLayer.FontStretch,
                          format?.FontWeight ?? SelectedLayer.FontWeight,
                          format?.FontStyle ?? SelectedLayer.FontStyle);

            if (Manager.Tool == this) // evaluates to false when Update() called in constructor
            {
                Manager.RaiseFillUpdate();
                Manager.RaiseStrokeUpdate();
            }

            Context.InvalidateSurface();
        }

        private void UpdateOptions(
            string     family, float     size, FontStretch stretch,
            FontWeight weight, FontStyle style)
        {
            if (_updatingOptions) return;

            _updatingOptions = true;

            Options.Set("font-family", family);
            Options.Set("font-size", size);

            var stretches = GetFontFaces()
                           .Select(f => f.Stretch)
                           .Distinct()
                           .ToArray();

            Options.SetValues("font-stretch", stretches);
            Options.Set("font-stretch", stretch);

            var weights = GetFontFaces()
                         .Where(f => f.Stretch == stretch)
                         .Select(f => f.Weight)
                         .Distinct()
                         .ToArray();

            Options.SetValues("font-weight", weights);
            Options.Set("font-weight", weight);

            var styles = GetFontFaces()
                        .Where(f => f.Stretch == stretch &&
                                    f.Weight == weight)
                        .Select(f => f.Style)
                        .Distinct()
                        .ToArray();

            Options.SetValues("font-style", styles);
            Options.Set("font-style", style);

            _updatingOptions = false;
        }

        #region Nested type: FontFace

        private struct FontFace : IEquatable<FontFace>
        {
            public FontFace(FontStretch stretch, FontStyle style, FontWeight weight) : this()
            {
                Weight = weight;
                Style = style;
                Stretch = stretch;
            }

            public FontStretch Stretch { get; }
            public FontStyle   Style   { get; }

            public FontWeight Weight { get; }

            public override bool Equals(object obj) { return obj is FontFace face && Equals(face); }

            public override int GetHashCode()
            {
                var hashCode = -404071;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + Weight.GetHashCode();
                hashCode = hashCode * -1521134295 + Style.GetHashCode();
                hashCode = hashCode * -1521134295 + Stretch.GetHashCode();

                return hashCode;
            }

            public override string ToString()
            {
                if (this == (FontStretch.Normal, FontStyle.Normal, FontWeight.Normal))
                    return "Regular";

                IEnumerable<string> G(FontFace f)
                {
                    if (f.Stretch != FontStretch.Normal) yield return f.Stretch.DePascalize();
                    if (f.Weight != FontWeight.Normal) yield return f.Weight.DePascalize();
                    if (f.Style != FontStyle.Normal) yield return f.Style.DePascalize();
                }

                return string.Join(" ", G(this));
            }

            public static bool operator ==(FontFace face1, FontFace face2) { return face1.Equals(face2); }

            public static implicit operator (FontStretch, FontStyle, FontWeight)(FontFace face)
            {
                return (face.Stretch, face.Style, face.Weight);
            }

            public static implicit operator FontFace((FontStretch, FontStyle, FontWeight) face)
            {
                return new FontFace(face.Item1, face.Item2, face.Item3);
            }

            public static bool operator !=(FontFace face1, FontFace face2) { return !(face1 == face2); }

            #region IEquatable<FontFace> Members

            public bool Equals(FontFace other)
            {
                return Weight == other.Weight &&
                       Style == other.Style &&
                       Stretch == other.Stretch;
            }

            #endregion
        }

        #endregion
    }
}