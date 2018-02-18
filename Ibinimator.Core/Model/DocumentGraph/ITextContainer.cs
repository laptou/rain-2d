using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Text;

namespace Ibinimator.Core.Model.DocumentGraph
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