using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Rain.Renderer.Direct2D;

using System.Threading.Tasks;

using Rain.Core.Model.Text;

using DW = SharpDX.DirectWrite;

namespace Rain.Renderer.DirectWrite
{
    internal class DirectWriteFontSource : IFontSource
    {
        private readonly DW.Factory        _factory;
        private readonly DW.FontCollection _fc;

        public DirectWriteFontSource(DW.Factory factory)
        {
            _factory = factory;
            _fc = _factory.GetSystemFontCollection(true);
        }

        #region IFontSource Members

        /// <inheritdoc />
        public void Dispose() { _fc?.Dispose(); }

        /// <inheritdoc />
        public IEnumerator<IFontFamily> GetEnumerator()
        {
            for (var i = 0; i < _fc.FontFamilyCount; i++)
                yield return new DirectWriteFontFamily(_factory, _fc.GetFontFamily(i));
        }

        /// <inheritdoc />
        public IFontFace GetFace(ITextInfo info)
        {
            using (var family = GetFamilyByName(info.FontFamily))
            {
                return family.GetFontFace(info.FontWeight, info.FontStyle, info.FontStretch);
            }
        }

        /// <inheritdoc />
        public IFontFamily GetFamilyByName(string name)
        {
            return new DirectWriteFontFamily(_factory, _fc.GetFamilyByName(name));
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion
    }

    internal class DirectWriteFontFamily : IFontFamily
    {
        private readonly DW.Factory    _factory;
        private readonly DW.FontFamily _ff;

        public DirectWriteFontFamily(DW.Factory factory, DW.FontFamily fontFamily)
        {
            _factory = factory;
            _ff = fontFamily;

            Name = fontFamily.FamilyNames.ToCurrentCulture();
        }

        #region IFontFamily Members

        /// <inheritdoc />
        public void Dispose() { _ff?.Dispose(); }

        /// <inheritdoc />
        public IFontFace GetClosestFontFace(FontWeight weight, FontStyle style, FontStretch stretch)
        {
            using (var fonts =
                _ff.GetMatchingFonts((DW.FontWeight) weight, (DW.FontStretch) stretch, (DW.FontStyle) style))
            {
                return fonts.FontCount == 0 ? null : new DirectWriteFontFace(fonts.GetFont(0));
            }
        }

        /// <inheritdoc />
        public IEnumerator<IFontFace> GetEnumerator()
        {
            for (var i = 0; i < _ff.FontCount; i++)
                yield return new DirectWriteFontFace(_ff.GetFont(i));
        }

        /// <inheritdoc />
        public IFontFace GetFontFace(FontWeight weight, FontStyle style, FontStretch stretch)
        {
            using (var font =
                _ff.GetFirstMatchingFont((DW.FontWeight) weight, (DW.FontStretch) stretch, (DW.FontStyle) style))

            {
                return new DirectWriteFontFace(font);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <inheritdoc />
        public string Name { get; }

        #endregion
    }

    public class DirectWriteFontFace : IFontFace
    {
        private readonly DW.FontFace _ff;

        public DirectWriteFontFace(DW.Font f)
        {
            _ff = new DW.FontFace(f);
            Stretch = (FontStretch) f.Stretch;
            Style = (FontStyle) f.Style;
            Weight = (FontWeight) f.Weight;

            GlyphCount = _ff.GlyphCount;


            var metrics = f.Metrics;
            Ascent = metrics.Ascent / (float) metrics.DesignUnitsPerEm;
            Descent = metrics.Descent / (float) metrics.DesignUnitsPerEm;
            CapHeight = metrics.CapHeight / (float) metrics.DesignUnitsPerEm;
            XHeight = metrics.XHeight / (float) metrics.DesignUnitsPerEm;
            LineGap = metrics.LineGap / (float) metrics.DesignUnitsPerEm;
        }

        #region IFontFace Members

        /// <inheritdoc />
        public void Dispose() { _ff?.Dispose(); }

        /// <inheritdoc />
        public float Ascent { get; }

        /// <inheritdoc />
        public float CapHeight { get; }

        /// <inheritdoc />
        public float Descent { get; }

        /// <inheritdoc />
        public int GlyphCount { get; }

        /// <inheritdoc />
        public float LineGap { get; }

        /// <inheritdoc />
        public FontStretch Stretch { get; }

        /// <inheritdoc />
        public FontStyle Style { get; }

        /// <inheritdoc />
        public FontWeight Weight { get; }

        /// <inheritdoc />
        public float XHeight { get; }

        #endregion
    }
}