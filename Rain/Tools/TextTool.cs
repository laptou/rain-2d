using System;
using System.Collections.Generic;
using System.ComponentModel;

using Ibinimator.Renderer.Direct2D;

using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Rain.Commands;
using Rain.Core;
using Rain.Core.Input;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Core.Utility;
using Rain.Resources;
using Rain.Utility;

using DW = SharpDX.DirectWrite;
using FontStretch = Rain.Core.Model.Text.FontStretch;
using FontStyle = Rain.Core.Model.Text.FontStyle;
using FontWeight = Rain.Core.Model.Text.FontWeight;

namespace Rain.Tools
{
    public sealed class TextTool : SelectionToolBase<ITextContainerLayer>
    {
        private readonly DW.Factory        _dwFactory;
        private readonly DW.FontCollection _dwFontCollection;

        private ICaret                                  _caret;
        private (Vector2 start, Vector2 end, long time) _drag;
        private bool                                    _focus;

        private (Vector2 position, bool down, long time, long previousTime) _mouse;
        private (int index, int length)                                     _selection;

        private RectangleF[] _selectionRects = new RectangleF[0];
        private bool         _updatingOptions;

        public TextTool(IToolManager manager) : base(manager)
        {
            Type = ToolType.Text;
            Status = "<b>Double Click</b> on canvas to create new text object. " +
                     "<b>Single Click</b> to select.";

            _dwFactory = new DW.Factory(DW.FactoryType.Shared);
            _dwFontCollection = _dwFactory.GetSystemFontCollection(true);

            var familyNames = Enumerable.Range(0, _dwFontCollection.FontFamilyCount)
                                        .Select(i =>
                                                {
                                                    using (var dwFontFamily =
                                                        _dwFontCollection.GetFontFamily(i))
                                                    {
                                                        return dwFontFamily
                                                              .FamilyNames.ToCurrentCulture();
                                                    }
                                                })
                                        .OrderBy(n => n);
            string defaultFamily;

            using (var defaultFont = _dwFontCollection.GetFontFamily(0))
            {
                defaultFamily = defaultFont.FamilyNames.ToCurrentCulture();
            }

            Options.Create<string>("font-family", ToolOptionType.Font, "Font Family")
                   .SetValues(familyNames.ToList())
                   .Set(defaultFamily);

            Options.Create<float>("font-size", ToolOptionType.Length, "Font Size")
                   .SetValues(new[]
                    {
                        8f, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 40, 44, 48, 72, 96,
                        120, 144, 288, 352
                    })
                   .SetUnit(Unit.Points)
                   .SetMinimum(6)
                   .SetMaximum(60000)
                   .Set(12);

            Options.Create<FontStretch>("font-stretch", ToolOptionType.Dropdown, "Stretch")
                   .Set(FontStretch.Normal)
                   .SetValues(new[]
                    {
                        FontStretch.Normal
                    });

            Options.Create<FontWeight>("font-weight", ToolOptionType.Dropdown, "Weight")
                   .Set(FontWeight.Normal)
                   .SetValues(new[]
                    {
                        FontWeight.Normal
                    });

            Options.Create<FontStyle>("font-style", ToolOptionType.Dropdown, "Style")
                   .Set(FontStyle.Normal)
                   .SetValues(new[]
                    {
                        FontStyle.Normal
                    });

            Options.OptionChanged += OnOptionChanged;

            Update();
        }

        public string Status
        {
            get => Get<string>();
            private set => Set(value);
        }

        public override void ApplyFill(IBrushInfo brush)
        {
            if (SelectedLayer == null ||
                brush == null) return;

            Format(new Format
                   {
                       Fill = brush,
                       Range = (_selection.index, _selection.length)
                   },
                   true);
        }

        public override void ApplyStroke(IPenInfo pen)
        {
            if (SelectedLayer == null ||
                pen == null) return;

            Format(new Format
                   {
                       Stroke = pen,
                       Range = _selection
                   },
                   true);
        }

        /// <inheritdoc />
        public override void Attach(IArtContext context)
        {
            context.Text += OnText;
            context.GainedFocus += OnGainedFocus;
            context.LostFocus += OnLostFocus;

            base.Attach(context);
        }

        /// <inheritdoc />
        public override void Detach(IArtContext context)
        {
            context.Text -= OnText;
            context.GainedFocus -= OnGainedFocus;
            context.LostFocus -= OnLostFocus;
            Dispose();

            base.Detach(context);
        }

        public void Dispose()
        {
            _caret?.Dispose();
            _dwFontCollection?.Dispose();
            _dwFactory?.Dispose();
        }

        public override void KeyDown(IArtContext context, KeyboardEvent evt)
        {
            if (SelectedLayer != null)
            {
                switch ((Key) evt.KeyCode)
                {
                    #region Navigation

                    case Key.Left:
                        Select(_selection.index - 1,
                               evt.ModifierState.Shift ? _selection.length + 1 : 0);

                        break;
                    case Key.Right:

                        Select(evt.ModifierState.Shift ? _selection.index : _selection.index + 1,
                               evt.ModifierState.Shift ? _selection.length + 1 : 0);

                        break;
                    /*case Key.Down:

                        if (evt.ModifierState.Shift)
                        {
                            var prev = text.Substring(0, _selection.index + _selection.length)
                                           .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selection.index + _selection.length)
                                          .IndexOf("\n", StringComparison.Ordinal) +
                                      _selection.index + _selection.length;

                            var next =
                                text.Substring(end + 1).IndexOf("\n", StringComparison.Ordinal) +
                                end + 1;

                            var pos = _selection.index + _selection.length - prev;

                            _selection.length = Math.Min(next, end + pos) - _selection.index;
                        }
                        else
                        {
                            var prev = text.Substring(0, _selection.index)
                                           .LastIndexOf("\n", StringComparison.Ordinal);

                            var end = text.Substring(_selection.index)
                                          .IndexOf("\n", StringComparison.Ordinal) +
                                      _selection.index;

                            var next =
                                text.Substring(end + 1).IndexOf("\n", StringComparison.Ordinal) +
                                end + 1;

                            var pos = _selection.index - prev;

                            _selection.index = Math.Min(next, end + pos);
                            _selection.length = 0;
                        }

                        break;
                    case Key.Up:

                        if (evt.ModifierState.Shift)
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

                        if (evt.ModifierState.Shift)
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
                        if (evt.ModifierState.Shift)
                            _selection.length += _selection.index;
                        else
                            _selection.length = 0;

                        _selection.index = 0;

                        break;*/
                    case Key.Escape:
                        if (_selection.length == 0)
                            SelectedLayer.Selected = false;

                        Select(_selection.index, 0);

                        break;

                    #endregion

                    #region Manipulation

                    case Key.Back:

                        if (_selection.index == 0 &&
                            _selection.length == 0) break;

                        if (_selection.length == 0)
                            Remove(--_selection.index, 1);
                        else
                            Remove(_selection.index, _selection.length);

                        Select(_selection.index, 0);

                        break;

                    case Key.Delete:

                        Remove(_selection.index, Math.Max(_selection.length, 1));

                        Select(_selection.index, 0);

                        break;

                    case Key.Return:
                        OnText(Context, new TextEvent(Environment.NewLine, evt.ModifierState));

                        break;

                    #endregion

                    #region Shorcuts

                    case Key.A when evt.ModifierState.Control:
                    {
                        if (SelectedLayer is ITextLayer textLayer)
                            Select(0, textLayer.Value.Length);
                    }

                        break;

                    case Key.B when evt.ModifierState.Control:
                        ToggleBold();

                        break;

                    case Key.I when evt.ModifierState.Control:
                        ToggleItalic();

                        break;

                    case Key.C when evt.ModifierState.Control:
                        CopyText();

                        break;

                    case Key.X when evt.ModifierState.Control:
                        CopyText();
                        Remove(_selection.index, _selection.length);

                        break;

                    case Key.V when evt.ModifierState.Control:
                        if (_selection.length > 0)
                            Remove(_selection.index, _selection.length);

                        var pasted = App.Dispatcher.Invoke(Clipboard.GetText);

                        Insert(_selection.index, pasted);

                        _selection.length = 0;
                        _selection.index += pasted.Length;

                        break;

                    #endregion

                    default:

                        return;
                }


                Update();
            }
        }

        public override void MouseDown(IArtContext context, ClickEvent evt)
        {
            var pos = context.ViewManager.ToArtSpace(evt.Position);
            _mouse = (pos, true, Time.Now, _mouse.time);
            _drag = (pos, pos, Time.Now);

            base.MouseDown(context, evt);
        }

        public override void MouseMove(IArtContext context, PointerEvent evt)
        {
            var pos = context.ViewManager.ToArtSpace(evt.Position);

            if (!(SelectedLayer is ITextLayer textLayer))
            {
                _mouse.position = pos;

                return;
            }

            _drag.end = pos;

            var bounds = Context.CacheManager.GetAbsoluteBounds(SelectedLayer);

            if (_mouse.down &&
                bounds.Contains(_drag.start))
            {
                var start = FromWorldSpace(_drag.start);
                var end = FromWorldSpace(_drag.end);

                var layout = Context.CacheManager.GetTextLayout(textLayer);

                var selectionStart = layout.GetPosition(start, out var isTrailingHit) +
                                     (isTrailingHit ? 1 : 0);

                var selectionEnd = layout.GetPosition(end, out isTrailingHit) +
                                   (isTrailingHit ? 1 : 0);

                _selection.index = Math.Min(selectionStart, selectionEnd);
                _selection.length = Math.Max(selectionStart, selectionEnd) - _selection.index;

                Update();
            }

            _mouse.position = pos;
        }

        public override void MouseUp(IArtContext context, ClickEvent evt)
        {
            var pos = context.ViewManager.ToArtSpace(evt.Position);
            _mouse.down = false;
            _drag.end = pos;

            if (SelectedLayer == null)
            {
                if (_mouse.time - _mouse.previousTime > Time.DoubleClick)
                {
                    base.MouseUp(context, evt);

                    return;
                }

                // if double click, make a new text object
                var text = new Text
                {
                    TextStyle = new TextInfo
                    {
                        FontFamily = Options.Get<string>("font-family"),
                        FontSize = Options.Get<float>("font-size"),
                        FontStyle = Options.Get<FontStyle>("font-style"),
                        FontStretch = Options.Get<FontStretch>("font-stretch"),
                        FontWeight = Options.Get<FontWeight>("font-weight")
                    },
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

                return;
            }

            if (!(SelectedLayer is ITextLayer textLayer)) return;

            var tpos = FromWorldSpace(pos);

            if (!Context.CacheManager.GetBounds(SelectedLayer).Contains(tpos))
            {
                base.MouseUp(context, evt);

                return;
            }

            if (_mouse.time - _mouse.previousTime <= Time.DoubleClick)
            {
                // double click, select more
                var layout = Context.CacheManager.GetTextLayout(textLayer);

                var rect = layout.Measure();

                if (rect.Contains(tpos))
                {
                    var str = textLayer.Value;
                    var start = _selection.index;
                    var end = start + _selection.length;

                    // move backwards until we encounter an alphanumeric character
                    while (start > 0 &&
                           !char.IsLetterOrDigit(str[start])) start--;

                    // continue moving backwards a non-alphanumeric character (word boundary)
                    while (start > 0 &&
                           char.IsLetterOrDigit(str[start])) start--;

                    while (end < str.Length &&
                           !char.IsLetterOrDigit(str[end])) end++;
                    while (end < str.Length &&
                           char.IsLetterOrDigit(str[end])) end++;

                    _selection.index = start;
                    _selection.length = end - start;
                    Update();
                }
            }
            else if (_drag.start == _drag.end)
            {
                _selection.length = 0;

                var layout = Context.CacheManager.GetTextLayout(textLayer);

                var textPosition = layout.GetPosition(tpos, out var isTrailingHit);

                _selection.index = textPosition + (isTrailingHit ? 1 : 0);

                UpdateCaret();
            }

            Update();
        }

        public void OnText(IArtContext sender, TextEvent evt)
        {
            if (SelectedLayer == null) return;

            if (_selection.length > 0)
                Remove(_selection.index, _selection.length);

            Insert(_selection.index, evt.Text);

            _selection.index += evt.Text.Length;
            _selection.length = 0;

            Update();
        }

        public override IBrushInfo ProvideFill()
        {
            var format = (SelectedLayer as ITextLayer)?.GetFormat(_selection.index);

            return format?.Fill ?? base.ProvideFill();
        }

        public override IPenInfo ProvideStroke()
        {
            var format = (SelectedLayer as ITextLayer)?.GetFormat(_selection.index);

            return format?.Stroke ?? base.ProvideStroke();
        }

        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            if (SelectedLayer == null) return;

            RenderBoundingBoxes(target, cache, view);

            target.Transform(SelectedLayer.AbsoluteTransform);

            if (_selection.length > 0)
                foreach (var selectionRect in _selectionRects)
                    target.FillRectangle(selectionRect,
                                         cache.GetBrush(nameof(EditorColors.TextHighlight)));

            target.Transform(MathUtils.Invert(SelectedLayer.AbsoluteTransform));
        }

        protected override void OnSelectionChanged(object sender, EventArgs e) { Update(); }

        private void CopyText()
        {
            if (SelectedLayer is ITextLayer text &&
                _selection.length > 0)
                Clipboard.SetText(text.Value.Substring(_selection.index, _selection.length));
        }

        private void Format(Format format, bool merge = false)
        {
            if (!(SelectedLayer is ITextLayer textLayer)) return;

            var history = Context.HistoryManager;
            var cmd = new ApplyFormatCommand(Context.HistoryManager.Position + 1,
                                             textLayer,
                                             format);

            if (merge) history.Merge(cmd, Time.DoubleClick);
            else history.Do(cmd);
        }

        private void Format(ITextInfo format, bool merge = false)
        {
            var history = Context.HistoryManager;
            var cmd = new ModifyTextCommand(Context.HistoryManager.Position + 1,
                                            SelectedLayer,
                                            format);

            if (merge) history.Merge(cmd, Time.DoubleClick);
            else history.Do(cmd);
        }

        private IEnumerable<FontFace> GetFontFaces()
        {
            if (!_dwFontCollection.FindFamilyName(Options.Get<string>("font-family"), out var index)
                )
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
            if (!(SelectedLayer is ITextLayer textLayer)) return;

            var history = Context.HistoryManager;
            var current =
                new InsertTextCommand(Context.HistoryManager.Position + 1, textLayer, text, index);

            history.Do(current);
        }

        private void OnGainedFocus(IArtContext sender, FocusEvent evt)
        {
            _focus = true;
            if (_caret != null) _caret.Visible = true;
            UpdateCaret();
        }

        private void OnLostFocus(IArtContext sender, FocusEvent evt)
        {
            _focus = false;
            _caret?.Dispose();
            _caret = null;
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_updatingOptions) return;

            var option = (ToolOptionBase) sender;

            if (SelectedLayer == null) return;
            if (!(e.PropertyName == nameof(ToolOption<object>.Value) ||
                  e.PropertyName == nameof(ToolOption<object>.Values)))
                return;

            switch (option)
            {
                case ToolOption<string> fontFamily when fontFamily.Id == "font-family":
                    if (_selection.length == 0)
                        Format(new TextInfo
                        {
                            FontFamily = fontFamily.Value
                        });
                    else
                        Format(new Format
                        {
                            FontFamilyName = fontFamily.Value,
                            Range = (_selection.index, _selection.length)
                        });

                    break;

                case ToolOption<FontStretch> fontStretch when fontStretch.Id == "font-stretch":
                    if (_selection.length == 0)
                        Format(new TextInfo
                        {
                            FontStretch = fontStretch.Value
                        });
                    else
                        Format(new Format
                        {
                            FontStretch = fontStretch.Value,
                            Range = (_selection.index, _selection.length)
                        });

                    break;

                case ToolOption<FontWeight> fontWeight when fontWeight.Id == "font-weight":
                    if (_selection.length == 0)
                        Format(new TextInfo
                        {
                            FontWeight = fontWeight.Value
                        });
                    else
                        Format(new Format
                        {
                            FontWeight = fontWeight.Value,
                            Range = (_selection.index, _selection.length)
                        });


                    break;

                case ToolOption<FontStyle> fontStyle when fontStyle.Id == "font-style":
                    if (_selection.length == 0)
                        Format(new TextInfo
                        {
                            FontStyle = fontStyle.Value
                        });
                    else
                        Format(new Format
                        {
                            FontStyle = fontStyle.Value,
                            Range = (_selection.index, _selection.length)
                        });

                    break;

                case ToolOption<float> fontSize when fontSize.Id == "font-size":
                    if (_selection.length == 0)
                        Format(new TextInfo
                        {
                            FontSize = fontSize.Value
                        });
                    else
                        Format(new Format
                        {
                            FontSize = fontSize.Value,
                            Range = (_selection.index, _selection.length)
                        });

                    break;
            }

            UpdateOptions(Options.Get<string>("font-family"),
                          Options.Get<float>("font-size"),
                          Options.Get<FontStretch>("font-stretch"),
                          Options.Get<FontWeight>("font-weight"),
                          Options.Get<FontStyle>("font-style"));

            Update();
        }

        private void Remove(int index, int length)
        {
            if (length == 0) return;

            if (SelectedLayer is ITextLayer text)
            {
                var history = Context.HistoryManager;
                var current = new RemoveTextCommand(Context.HistoryManager.Position + 1,
                                                    text,
                                                    text.Value.Substring(index, length),
                                                    index);
                history.Do(current);
            }
        }

        private void Select(int start, int length)
        {
            if (SelectedLayer is ITextLayer text)
            {
                var maxLength = text.Value?.Length ?? 0;

                _selection = (start, length);
                _selection.index = MathUtils.Clamp(0, maxLength, _selection.index);
                _selection.length =
                    MathUtils.Clamp(0, maxLength - _selection.index, _selection.length);

                Update();
            }
        }

        private void ToggleBold()
        {
            if (SelectedLayer is ITextLayer text &&
                _selection.length > 0)
            {
                var format = text.GetFormat(_selection.index);
                var weight = format?.FontWeight ?? SelectedLayer.TextStyle.FontWeight;

                Format(new Format
                {
                    FontWeight =
                        weight == FontWeight.Normal
                            ? FontWeight.Bold
                            : FontWeight.Normal,
                    Range = (_selection.index, _selection.length)
                });
            }
            else
            {
                var weight = SelectedLayer.TextStyle.FontWeight;
                Format(new TextInfo
                {
                    FontWeight = weight == FontWeight.Normal
                                     ? FontWeight.Bold
                                     : FontWeight.Normal
                });
            }
        }

        private void ToggleItalic()
        {
            if (SelectedLayer is ITextLayer text &&
                _selection.length > 0)
            {
                var format = text.GetFormat(_selection.index);
                var style = format?.FontStyle ?? SelectedLayer.TextStyle.FontStyle;

                Format(new Format
                {
                    FontStyle =
                        style == FontStyle.Normal
                            ? FontStyle.Italic
                            : FontStyle.Normal,
                    Range = (_selection.index, _selection.length)
                });
            }
            else
            {
                var style = SelectedLayer.TextStyle.FontStyle;
                Format(new TextInfo
                {
                    FontStyle = style == FontStyle.Normal
                                    ? FontStyle.Italic
                                    : FontStyle.Normal
                });
            }
        }

        private void Update()
        {
            if (SelectedLayer == null) return;

            if (SelectedLayer is ITextLayer text)
            {
                var layout = Context.CacheManager.GetTextLayout(text);

                UpdateCaret();

                _selectionRects = _selection.length > 0
                                      ? layout.MeasureRange(_selection.index, _selection.length)
                                      : new RectangleF[0];

                var format = text.GetFormat(_selection.index);

                UpdateOptions(format?.FontFamilyName ?? SelectedLayer.TextStyle.FontFamily,
                              format?.FontSize ?? SelectedLayer.TextStyle.FontSize,
                              format?.FontStretch ?? SelectedLayer.TextStyle.FontStretch,
                              format?.FontWeight ?? SelectedLayer.TextStyle.FontWeight,
                              format?.FontStyle ?? SelectedLayer.TextStyle.FontStyle);
            }
            else
            {
                UpdateOptions(SelectedLayer.TextStyle.FontFamily,
                              SelectedLayer.TextStyle.FontSize,
                              SelectedLayer.TextStyle.FontStretch,
                              SelectedLayer.TextStyle.FontWeight,
                              SelectedLayer.TextStyle.FontStyle);
            }

            if (Manager.Tool == this) // evaluates to false when Update() called in constructor
            {
                Manager.RaiseFillUpdate();
                Manager.RaiseStrokeUpdate();
            }

            Context.InvalidateRender();
        }

        private void UpdateCaret()
        {
            if (!(SelectedLayer is ITextLayer textLayer))
            {
                if (_caret != null)
                    _caret.Visible = false;

                return;
            }

            if (!_focus)
            {
                _caret?.Dispose();
                _caret = null;

                return;
            }

            var layout = Context.CacheManager.GetTextLayout(textLayer);

            var metrics = layout.MeasurePosition(_selection.index + _selection.length);

            if (_caret == null)
            {
                try
                {
                    _caret = Context.Create<ICaret>(0, (int) metrics.Height);
                }
                catch
                {
                    return;
                }

                // if it's still null, then we can't create a caret right now.
                // give up i guess
                if (_caret == null)
                    return;

                _caret.Visible = true;
            }

            _caret.Position =
                ToWorldSpace(new Vector2(metrics.Left,
                                         (metrics.Top + metrics.Height - metrics.Baseline) * 1.5f));
        }

        private void UpdateOptions(
            string family, float size, FontStretch stretch, FontWeight weight, FontStyle style)
        {
            if (_updatingOptions) return;

            _updatingOptions = true;

            Options.Set("font-family", family);
            Options.Set("font-size", size);

            var stretches = GetFontFaces().Select(f => f.Stretch).Distinct().ToArray();

            Options.GetOption<FontStretch>("font-stretch").SetValues(stretches);
            Options.Set("font-stretch", stretch);

            var weights = GetFontFaces()
                         .Where(f => f.Stretch == stretch)
                         .Select(f => f.Weight)
                         .Distinct()
                         .ToArray();

            Options.GetOption<FontWeight>("font-weight").SetValues(weights);
            Options.Set("font-weight", weight);

            var styles = GetFontFaces()
                        .Where(f => f.Stretch == stretch && f.Weight == weight)
                        .Select(f => f.Style)
                        .Distinct()
                        .ToArray();

            Options.GetOption<FontStyle>("font-style").SetValues(styles);
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
            public FontStyle Style { get; }
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
                if (this == (FontStretch.Normal, FontStyle.Normal,
                                FontWeight.Normal))
                    return "Regular";

                IEnumerable<string> G(FontFace f)
                {
                    if (f.Stretch != FontStretch.Normal)
                        yield return f.Stretch.DePascalize();
                    if (f.Weight != FontWeight.Normal)
                        yield return f.Weight.DePascalize();
                    if (f.Style != FontStyle.Normal)
                        yield return f.Style.DePascalize();
                }

                return string.Join(" ", G(this));
            }

            public static bool operator ==(FontFace face1, FontFace face2)
            {
                return face1.Equals(face2);
            }

            public static implicit operator (FontStretch, FontStyle, FontWeight)(FontFace face)
            {
                return (face.Stretch, face.Style, face.Weight);
            }

            public static implicit operator FontFace((FontStretch, FontStyle, FontWeight) face)
            {
                return new FontFace(face.Item1, face.Item2, face.Item3);
            }

            public static bool operator !=(FontFace face1, FontFace face2)
            {
                return !(face1 == face2);
            }

            #region IEquatable<FontFace> Members

            public bool Equals(FontFace other)
            {
                return Weight == other.Weight && Style == other.Style && Stretch == other.Stretch;
            }

            #endregion
        }

        #endregion
    }
}