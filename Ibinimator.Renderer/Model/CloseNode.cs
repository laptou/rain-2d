using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public class CloseNode : PathNode
    {
        public bool Open
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}