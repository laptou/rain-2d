using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IbiFormatter
{
    public interface IFormatter<T>
    {
        Stream Serialize(T graph);
        T Deserialize(Stream source);
    }

    public abstract class FormatterBase<T> : IFormatter<T>
    {
        private Dictionary<Type, MemberInfo> _typeGraph;

        protected FormatterBase()
        {
            Type type = typeof(T);

            Graph(type);
        }

        private void Graph(Type type)
        {
            if (_typeGraph.ContainsKey(type)) return;

            if (type.IsElementary()) return;

            var graph = new Dictionary<string, MemberInfo>();

            foreach (var property in type.GetRuntimeProperties())
                graph.Add(property.Name, property);

            foreach (var field in type.GetRuntimeFields())
                graph.Add(field.Name, field);
        }

        protected abstract void Write<TV>(TV value, string key);
        protected abstract void Write<TV>(TV value, long key);
        protected abstract void Write<TV>(TV value, int key);
        protected abstract void Write<TV>(TV value, short key);
        protected abstract void Write<TV>(TV value, byte key);

        public abstract Stream Serialize(T graph);

        public abstract T Deserialize(Stream source);
    }
}
