using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public interface ISolidColorBrush : IBrush
    {
        Color Color { get; set; }
    }
}