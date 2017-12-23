using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface ICaret : IDisposable
    {
        void Hide();
        void Show();
    }
}