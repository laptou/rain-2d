using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Formatter.Svg.Paint
{
    public class ReferencePaint : Paint
    {
        public virtual Iri Reference { get; set; }

        /// <inheritdoc />
        public override string ToInline() { return $"url({Reference})"; }
    }
}