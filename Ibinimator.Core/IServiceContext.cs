using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Text;

namespace Ibinimator.Core
{
    public interface IServiceContext
    {
        ICaret CreateCaret(int height);
    }
}