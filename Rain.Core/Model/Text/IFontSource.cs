using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Text
{
    public interface IFontSource : IDisposable, IEnumerable<IFontFamily>
    {
        IFontFamily GetFamilyByName(string name);
        IFontFace GetFace(ITextInfo info);
    }

    public interface IFontFamily : IDisposable, IEnumerable<IFontFace>
    {
        string Name { get; }

        IFontFace GetClosestFontFace(FontWeight weight, FontStyle style, FontStretch stretch);

        IFontFace GetFontFace(FontWeight weight, FontStyle style, FontStretch stretch);
    }

    public interface IFontFace : IDisposable
    {
        /// <summary>
        ///     The distance from the top of the font character alignment box to the English baseline
        ///     in ems.
        /// </summary>
        float Ascent { get; }

        /// <summary>
        ///     The distance from the English baseline to the top of a typical English capital,
        ///     such as H, in ems.
        /// </summary>
        float CapHeight { get; }

        /// <summary>
        ///     The distance from the bottom of the font character alignment box to the English baseline
        ///     in ems.
        /// </summary>
        float Descent { get; }

        int GlyphCount { get; }

        /// <summary>
        ///     The recommended gap between lines in ems.
        /// </summary>
        float LineGap { get; }

        FontStretch Stretch { get; }
        FontStyle Style { get; }
        FontWeight Weight { get; }

        /// <summary>
        ///     The distance from the English baseline to the top of a typical English lowercase letter,
        ///     such as x, in ems.
        /// </summary>
        float XHeight { get; }
    }
}