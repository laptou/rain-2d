using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AnimatableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UndoableAttribute : Attribute
    {
        public Action OnUndoAction { get; set; }
    }
}