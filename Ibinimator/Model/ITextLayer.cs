using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public interface ITextLayer : IFilledLayer, IStrokedLayer
    {
        string FontFamilyName { get; set; }
        float FontSize { get; set; }
        SharpDX.DirectWrite.FontStretch FontStretch { get; set; }
        SharpDX.DirectWrite.FontStyle FontStyle { get; set; }
        SharpDX.DirectWrite.FontWeight FontWeight { get; set; }
        string Value { get; set; }
        SharpDX.DirectWrite.TextLayout GetLayout(SharpDX.DirectWrite.Factory dwFactory);
    }
}