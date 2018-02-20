using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Text;

namespace Rain.Core
{
    public interface IServiceContext
    {
        ICaret CreateCaret(int height);
    }
}