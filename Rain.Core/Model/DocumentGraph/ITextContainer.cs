using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Text;

namespace Rain.Core.Model.DocumentGraph
{
    public interface ITextContainer
    {
        ITextInfo TextStyle { get; set; }
    }

    public interface ITextContainerLayer : ITextContainer, ILayer
    {
        event EventHandler TextStyleChanged;
    }
}