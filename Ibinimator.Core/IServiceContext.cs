using System;
using System.Collections.Generic;
using System.Text;

namespace Ibinimator.Core
{
    public interface IServiceContext
    {
        ICaret CreateCaret(int height);
    }
}
