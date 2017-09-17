using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DW = SharpDX.DirectWrite;

namespace Ibinimator.Model
{
    public sealed class Format
    {
        private bool _subscript;
        private bool _superscript;

        public float? CharacterSpacing { get; set; }

        public BrushInfo Fill { get; set; }

        public string FontFamilyName { get; set; }

        public float? FontSize { get; set; }

        public DW.FontStretch? FontStretch { get; set; }

        public DW.FontStyle? FontStyle { get; set; }

        public DW.FontWeight? FontWeight { get; set; }

        public float? Kerning { get; set; }

        public DW.TextRange Range { get; set; }

        public BrushInfo Stroke { get; set; }

        public StrokeInfo StrokeInfo { get; set; }

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
                StrokeInfo = StrokeInfo,
                CharacterSpacing = CharacterSpacing,
                Kerning = Kerning,
                Range = Range
            };
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
                Stroke = f.Stroke ?? Stroke,
                StrokeInfo = f.StrokeInfo ?? StrokeInfo
            };
        }
    }
}