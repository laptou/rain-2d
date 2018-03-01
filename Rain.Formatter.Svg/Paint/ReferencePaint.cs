using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Formatter.Svg.Paint
{
    public class ReferencePaint : Paint
    {
        public virtual Uri Reference { get; set; }

        /// <inheritdoc />
        public override string ToInline() { return $"url({Reference})"; }
    }
}