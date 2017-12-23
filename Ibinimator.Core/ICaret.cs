using System;
using System.Collections.Generic;
using System.Text;

namespace Ibinimator.Core
{
    public interface ICaret : IDisposable
    {
        void Show();
        void Hide();
    }
}
