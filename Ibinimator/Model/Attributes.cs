using System;

namespace Ibinimator.Model
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class AnimatableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class UndoableAttribute : Attribute
    {
        public Action OnUndoAction { get; set; }
    }
}
