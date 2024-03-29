﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Core.Model.DocumentGraph
{
    public class NodeReader
    {
        public static IEnumerable<Node> ToNodes(Document doc)
        {
            var root = new Node(null, doc, null);

            yield return root;

            foreach (var swatch in doc.Swatches)
                yield return new Node(swatch.Name, swatch, root);

            foreach (var layer in doc.Root.SubLayers.Reverse())
                foreach (var node in ToNodes(layer, root))
                    yield return node;
        }

        public static IEnumerable<Node> ToNodes(IBrushInfo brush, Node ancestor)
        {
            var node = new Node(brush.Name, brush, ancestor);

            yield return node;

            if (brush is GradientBrushInfo gradient)
                foreach (var stop in gradient.Stops)
                    yield return new Node(null, stop, node);
        }

        public static IEnumerable<Node> ToNodes(IPenInfo pen, Node ancestor)
        {
            var node = new Node(null, pen, ancestor);

            yield return node;

            if (pen.Brush != null)
                foreach (var n in ToNodes(pen.Brush, node))
                    yield return n;
        }

        public static IEnumerable<Node> ToNodes(ILayer layer, Node ancestor)
        {
            var root = new Node(layer.Name, layer, ancestor);

            yield return root;

            if (layer is IFilledLayer filled &&
                filled.Fill != null)
                foreach (var node in ToNodes(filled.Fill, root))
                    yield return node;

            if (layer is IStrokedLayer stroked &&
                stroked.Stroke != null)
                foreach (var node in ToNodes(stroked.Stroke, root))
                    yield return node;

            if (layer is ITextLayer text &&
                text.Value != null)
                foreach (var node in ToNodes(text, root))
                    yield return node;

            if (layer is IContainerLayer container)
                foreach (var sublayer in container.SubLayers.Reverse())
                    foreach (var node in ToNodes(sublayer, root))
                        yield return node;
        }

        public static IEnumerable<Node> ToNodes(ITextLayer layer, Node ancestor)
        {
            var node = new Node(layer.Name, layer, ancestor);

            foreach (var format in layer.Formats)
                yield return new Node(null, format, node);
        }
    }

    [DebuggerDisplay("#{Rank}::{Id}::{Target.GetType()} < {Parent?.Target.GetType()}")]
    public class Node
    {
        public Node(string name, object target, Node parent)
        {
            Target = target;

            if (parent != null)
            {
                Parent = parent;
                Rank = parent.Rank + 1;
            }

            Name = name;
        }

        public string Id
        {
            get
            {
                var prefix = (char) (97 + Math.Abs(Name?.GetHashCode() ?? 0) % 26);
                var suffix = unchecked((uint) Target.GetHashCode()).ToString();

                return string.Join("_", prefix, Name, suffix);
            }
        }

        public string Name { get; }
        public Node Parent { get; }
        public int Rank { get; }
        public object Target { get; }
    }
}