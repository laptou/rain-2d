using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;

namespace Rain.Core.Model.DocumentGraph
{
    public class Text : Layer, ITextLayer
    {
        private bool _suppressed;

        public Text()
        {
            Value = "";
            TextStyle = new TextInfo();
            Stroke = new PenInfo();

            Formats.CollectionChanged += (s, e) => RaiseLayoutChanged();
        }

        public bool IsBlock
        {
            get => Get<bool>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        /*public DW.ParagraphAlignment ParagraphAlignment
        {
            get => Get<DW.ParagraphAlignment>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public DW.TextAlignment TextAlignment
        {
            get => Get<DW.TextAlignment>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }*/

        public Format GetFormat(int position, out int index)
        {
            var sorted = Formats.OrderBy(f => f.Range.Index).ToArray();

            index = 0;

            do
            {
                index++;
            }
            while (index < sorted.Length && sorted[index].Range.Index <= position);

            var format = sorted.ElementAtOrDefault(--index);

            if (format == null) return null;

            return format.Range.Index + format.Range.Length > position &&
                   position >= format.Range.Index
                       ? format
                       : null;
        }

        /// <inheritdoc />
        public override void RestoreNotifications()
        {
            _suppressed = false;

            base.RestoreNotifications();
        }

        /// <inheritdoc />
        public override void SuppressNotifications()
        {
            _suppressed = true;

            base.SuppressNotifications();
        }

        protected void RaiseFillChanged()
        {
            if (_suppressed) return;

            FillChanged?.Invoke(this, null);
        }

        protected void RaiseGeometryChanged()
        {
            if (_suppressed) return;

            GeometryChanged?.Invoke(this, null);

            RaiseBoundsChanged();
        }

        protected void RaiseLayoutChanged()
        {
            if (_suppressed) return;

            LayoutChanged?.Invoke(this, null);

            RaiseGeometryChanged();
        }

        protected void RaiseStrokeChanged()
        {
            if (_suppressed) return;

            StrokeChanged?.Invoke(this, null);
        }

        protected void RaiseTextStyleChanged()
        {
            if (_suppressed) return;

            TextStyleChanged?.Invoke(this, null);

            RaiseLayoutChanged();
        }

        private void OnTextStylePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseTextStyleChanged();
        }

        #region ITextLayer Members

        public event EventHandler FillChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler LayoutChanged;
        public event EventHandler StrokeChanged;

        /// <inheritdoc />
        public event EventHandler TextStyleChanged;

        public void ClearFormat()
        {
            lock (Formats)
            {
                Formats.Clear();
            }

            RaiseLayoutChanged();
        }

        public override RectangleF GetBounds(IArtContext ctx)
        {
            if (IsBlock)
                return new RectangleF(0, 0, Width, Height);

            return ctx.CacheManager.GetTextLayout(this).Measure();
        }

        public Format GetFormat(int position) { return GetFormat(position, out var _); }

        public IGeometry GetGeometry(IArtContext ctx)
        {
            var layout = ctx.CacheManager.GetTextLayout(this);

            if (layout.Text.Length == 0) return null;


            var geometries = new List<IGeometry>();

            for (var i = 0; i < layout.GetGlyphCount(); i += layout.GetGlyphCountForGeometry(i))
                geometries.Add(layout.GetGeometryForGlyphRun(i));

            return ctx.RenderContext.CreateGeometryGroup(geometries.ToArray());
        }

        public ITextLayout GetLayout(IArtContext ctx)
        {
            var layout = ctx.RenderContext.CreateTextLayout();

            layout.FontSize = TextStyle.FontSize;
            layout.FontStyle = TextStyle.FontStyle;
            layout.FontWeight = TextStyle.FontWeight;
            layout.FontStretch = TextStyle.FontStretch;
            layout.FontFamily = TextStyle.FontFamily;
            layout.InsertText(0, Value);

            foreach (var format in Formats)
                layout.SetFormat(format);

            return layout;
        }

        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            if (!(this is T t)) return default;
            if (minimumDepth > 0) return default;

            point = Vector2.Transform(point, MathUtils.Invert(Transform));

            var layout = cache.GetTextLayout(this);

            return layout.Hit(point) ? t : default;
        }

        public void InsertText(int position, string text)
        {
            // Inserting inside of the lock leads to RaiseGeometryChanged()
            // which triggers a re-render, which then asks for the layout
            // which locks the _formats
            Value = Value.Insert(position, text);

            lock (Formats)
            {
                // expand the length of the format
                var format = GetFormat(position, out var index);

                if (format != null)
                    format.Range = (format.Range.Index, format.Range.Length + text.Length);

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    format = Formats[index];
                    format.Range = (format.Range.Index + text.Length, format.Range.Length);
                    index++;
                }
            }

            RaiseLayoutChanged();
        }

        public void RemoveText(int position, int range)
        {
            Value = Value.Remove(position, range);

            lock (Formats)
            {
                var current = position;
                var end = position + range;
                var index = 0;

                while (current < end)
                {
                    // expand the length of the format
                    var format = GetFormat(current, out var idx);

                    if (format == null)
                    {
                        current++;

                        continue;
                    }

                    index = idx;

                    var fstart = format.Range.Index;
                    var len = fstart - position;
                    var fend = fstart + format.Range.Length;

                    if (len <= 0 &&
                        fend <= end) Formats.Remove(format);
                    else if (len <= 0 &&
                             fend > end)
                        format.Range = (fstart, fend - fstart - range);
                    else
                        format.Range = (fstart, len);

                    current++;
                }

                index++;

                // offset all of the formats that come after this
                while (index < Formats.Count)
                {
                    var format = Formats[index];
                    format.Range = (format.Range.Index - range, format.Range.Length);
                    index++;
                }
            }
        }

        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            // grabbing the value here avoids jitter if the transform is changed during the rendering
            var transform = Transform;

            target.Transform(transform);

            var layout = cache.GetTextLayout(this);

            for (var i = 0; i < layout.GetGlyphCount(); i += layout.GetGlyphCountForGeometry(i))
            {
                var geom = layout.GetGeometryForGlyphRun(i);
                var fill = layout.GetBrushForGlyph(i) ?? cache.GetFill(this);
                var pen = layout.GetPenForGlyph(i) ?? cache.GetStroke(this);

                if (fill != null)
                    target.FillGeometry(geom, fill);

                if (pen?.Brush != null)
                    target.DrawGeometry(geom, pen, pen.Width * view.Zoom);
            }

            target.Transform(MathUtils.Invert(transform));
        }

        public void SetFormat(Format format)
        {
            var range = format.Range;
            var start = range.Index;
            var current = range.Index;
            var end = range.Index + range.Length;

            if (start == end) return;

            lock (Formats)
            {
                while (current < end)
                {
                    var oldFormat = GetFormat(current);

                    if (oldFormat != null)
                    {
                        var oldRange = oldFormat.Range;
                        var oStart = oldRange.Index;
                        var oLen = oldRange.Length;
                        var oEnd = oStart + oLen;

                        int nStart;
                        int nLen;

                        var newFormat = oldFormat.Merge(format);

                        if (oStart < start) nStart = start;
                        else nStart = oStart;

                        if (oEnd > end) nLen = end - nStart;
                        else nLen = oEnd - nStart;

                        newFormat.Range = (nStart, nLen);

                        var nEnd = nStart + nLen;

                        if (start > oStart)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = (oStart, start - oStart);
                            Formats.Add(iFormat);
                        }

                        if (nEnd < oEnd)
                        {
                            var iFormat = oldFormat.Clone();
                            iFormat.Range = (nEnd, oEnd - nEnd);
                            Formats.Add(iFormat);
                        }

                        if (nStart > start)
                        {
                            var iFormat = newFormat.Clone();
                            iFormat.Range = (start, nStart - start);
                            Formats.Add(iFormat);
                        }

                        current = start = Math.Max(nEnd, oEnd);

                        Formats.Remove(oldFormat);
                        Formats.Add(newFormat);
                    }
                    else
                    {
                        current++;
                    }
                }

                if (start < end)
                {
                    format.Range = (start, end - start);
                    Formats.Add(format);
                }
            }
        }

        public override string DefaultName => $@"Text ""{Value.Truncate(30)}""";

        public IBrushInfo Fill
        {
            get => Get<IBrushInfo>();
            set
            {
                Fill?.RemoveReference();
                Set(value);
                Fill?.AddReference();
                RaiseFillChanged();
            }
        }

        public ObservableList<Format> Formats { get; } = new ObservableList<Format>();

        public float Height
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        public ObservableList<float> Offsets => Get<ObservableList<float>>();

        public IPenInfo Stroke
        {
            get => Get<IPenInfo>();
            set
            {
                Stroke?.RemoveReference();
                Set(value);
                Stroke?.AddReference();
                RaiseStrokeChanged();
            }
        }

        /// <inheritdoc />
        public ITextInfo TextStyle
        {
            get => Get<ITextInfo>();
            set
            {
                if (TextStyle != null) TextStyle.PropertyChanged -= OnTextStylePropertyChanged;
                Set(value);
                if (TextStyle != null) TextStyle.PropertyChanged += OnTextStylePropertyChanged;
                RaiseTextStyleChanged();
            }
        }

        public string Value
        {
            get => Get<string>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(DefaultName));
                RaiseLayoutChanged();
            }
        }

        public float Width
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseLayoutChanged();
            }
        }

        #endregion
    }
}