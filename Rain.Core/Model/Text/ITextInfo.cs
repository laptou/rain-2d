using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Text
{
    public interface ITextInfo : IModel
    {
        float Baseline { get; set; }
        string FontFamily { get; set; }
        float FontSize { get; set; }
        FontStretch FontStretch { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }
    }
}