using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    [DebuggerDisplay("Start = {Range.Index}, Length = {Range.Length}")]
    public sealed class Format
    {
        private bool _subscript;
        private bool _superscript;

        public float? CharacterSpacing { get; set; }

        public IBrushInfo Fill { get; set; }

        public string FontFamilyName { get; set; }

        public float? FontSize { get; set; }

        public FontStretch? FontStretch { get; set; }

        public FontStyle? FontStyle { get; set; }

        public FontWeight? FontWeight { get; set; }

        public float? Kerning { get; set; }

        public (int Index, int Length) Range { get; set; }

        public IPenInfo Stroke { get; set; }

        public bool Subscript
        {
            get => _subscript;
            set
            {
                _subscript = value;
                if (value) Superscript = false;
            }
        }

        public bool Superscript
        {
            get => _superscript;
            set
            {
                _superscript = value;
                if (value) Subscript = false;
            }
        }

        public Format Clone()
        {
            return new Format
            {
                Superscript = Superscript,
                Subscript = Subscript,
                FontSize = FontSize,
                FontFamilyName = FontFamilyName,
                FontStyle = FontStyle,
                FontStretch = FontStretch,
                FontWeight = FontWeight,
                Fill = Fill,
                Stroke = Stroke,
                CharacterSpacing = CharacterSpacing,
                Kerning = Kerning,
                Range = Range
            };
        }

        public override string ToString()
        {
            var f = this;

            return $"{f.Range.Index} + {f.Range.Length}" +
                   $" -> {f.Range.Index + f.Range.Length}: " +
                   $"{f.Fill?.ToString() ?? "none"} {f.FontStyle} {f.FontWeight}";
        }

        public Format Union(Format f)
        {
            return new Format
            {
                FontFamilyName = f.FontFamilyName ?? FontFamilyName,
                FontStyle = f.FontStyle ?? FontStyle,
                FontSize = f.FontSize ?? FontSize,
                FontStretch = f.FontStretch ?? FontStretch,
                FontWeight = f.FontWeight ?? FontWeight,
                Subscript = f.Subscript || Subscript,
                Superscript = f.Superscript || Superscript,
                Fill = f.Fill ?? Fill,
                Stroke = f.Stroke ?? Stroke
            };
        }
    }
}